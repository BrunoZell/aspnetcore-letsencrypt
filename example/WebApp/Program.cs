using AspNetCore.LetsEncrypt;
using AspNetCore.LetsEncrypt.Options;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp {
    public class Program {
        public static void Main(string[] args) {
            var letsEncrypt = new LetsEncrypt(args);
            letsEncrypt.EnsureHttps();

            CreateWebHostBuilder(args, letsEncrypt.Options.Certificate)
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, Certificate httpsCertificate) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls() // Remove warning for overridden settings by UseKestrel.
                .UseKestrel(options => {
                    options.ListenAnyIP(80);
                    options.ListenAnyIP(443, o => o.UseHttps(httpsCertificate.Filename, httpsCertificate.Password));
                })
                .Configure(app => {
                    // Return "Hello World!" on every request.
                    // For simplicity we don't use a Startup-class in this example.
                    app.Run(context => context.Response.WriteAsync("Hello World!"));
                });
    }
}
