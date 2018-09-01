using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using WebApp.Options;

namespace WebApp {
    public class Program {
        public static void Main(string[] args) {
            var letsEncrypt = new LetsEncrypt(args);
            letsEncrypt.EnsureHttps();

            CreateWebHostBuilder(args, letsEncrypt.Options.Certificate)
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, Certificate httpsCertificate)
            => WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options => {
                    options.ListenAnyIP(80);
                    options.ListenAnyIP(443, o => o.UseHttps(httpsCertificate.Filename, httpsCertificate.Password));
                })
                .UseStartup<Startup>();
    }
}
