using AspNetCore.LetsEncrypt;
using AspNetCore.LetsEncrypt.Persistence;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Cryptography.X509Certificates;

namespace WebApp {
    public static class Program {
        public static void Main(string[] args)
        {
            var store = new InMemoryCertificateStore();
            new LetsEncryptBuilder()
                .WithConfiguration(BuildConfiguration(args).GetSection("LetsEncrypt")) // See appsettings.json for structure
                .WithOptions(options => {
                    // Here you can overwrite some options provided by WithConfiguration(..).
                    // Or don't use WithConfiguration(..) at all and configure everything in code here.
                    options.Hostname = "3bc29e11.ngrok.io";
                })
                .ConfigureWebHost(builder => {
                    // For the ACME chellenge a small Kestrel-server is hosted before the actual web app starts up.
                    // For advanced usage scenarios you have access to the IWebHostBuilder
                })
                .OnError(options => {
                    // Get more information about the error
                    Console.WriteLine(options.Exception.InnerException.Message);
                
                    // Whether to start the web app configured in ContinueWith(..) anyways
                    options.Continue = true;
                })
                .UseCertificateLoader(store.Loader)
                .UseCertificateSaver(store.Saver)
                .ContinueWith(certificate => CreateWebHostBuilder(args, certificate).Build())
                .Build()
                .Run(); // OR .RunAsync()
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args, X509Certificate2 certificate) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls() // Removes warning for overridden settings by UseKestrel.
                .UseKestrel(options => {
                    // Listen to http traffic as wall as https traffice with the generated certificate.
                    // By now the certificate is stored in a pfx-file under the configured path.
                    options.ListenAnyIP(80);
                    options.ListenAnyIP(443, o => o.UseHttps(certificate));
                })
                .Configure(app => {
                    // Return "Hello World!" on every request.
                    // For simplicity we don't use a Startup-class in this example.
                    app.Run(context => context.Response.WriteAsync("Hello World!"));
                });

        private static IConfiguration BuildConfiguration(string[] args) =>
            new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.Development.json", true, true)
                .Build();
    }
}
