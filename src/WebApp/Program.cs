using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using WebApp.Options;

namespace WebApp {
    public class Program {
        public static void Main(string[] args) {
            CreateAcmeHostBuilder()
                .Build()
                .Run();

            CreateWebHostBuilder(args)
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateAcmeHostBuilder()
           => new WebHostBuilder()
                .UseKestrel(options => options.ListenAnyIP(80))
                .UseStartup<StartupAcme>();

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options => {
                    var letsencryptOptions = options.ConfigurationLoader.Configuration
                        .GetSection(LetsEncryptOptions.SectionName)
                        .Get<LetsEncryptOptions>();

                    options.ListenAnyIP(80);
                    options.ListenAnyIP(443, o => o.UseHttps(letsencryptOptions.Certificate.Filename, letsencryptOptions.Certificate.Password));
                })
                .UseStartup<Startup>();
    }
}
