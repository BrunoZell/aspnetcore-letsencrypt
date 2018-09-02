using System;
using AspNetCore.LetsEncrypt;
using AspNetCore.LetsEncrypt.Options;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp {
    public static class Program {
        public static void Main(string[] args) {
            var configuration = BuildConfiguration(args);

            new LetsEncryptBuilder()
                .WithConfiguration(configuration.GetSection("LetsEncrypt"))
                .WithOptions(o => {
                    // Overwrite some options provided by WithConfiguration
                    //o.Hostname = "";
                })
                .ConfigureWebHost(builder => {
                })
                .OnError(o => {
                    o.Continue = false;
                    Console.WriteLine(o.Exception.Message);
                })
                .ContinueWith(cert => CreateWebHostBuilder(args, cert).Build())
                .Build()
                .Run(); // OR .RunAsync()
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args, Certificate sslCertificate) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls() // Removes warning for overridden settings by UseKestrel.
                .UseKestrel(options => {
                    options.ListenAnyIP(80);
                    options.ListenAnyIP(443, o => o.UseHttps(sslCertificate.Filename, sslCertificate.Password));
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
