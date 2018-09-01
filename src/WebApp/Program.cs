using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApp.Extensions;
using WebApp.Internal;
using WebApp.Options;

namespace WebApp {
    public class Program {
        public static void Main(string[] args) {
            CreateAcmeHostBuilder(args)
                .Build()
                .Run();

            CreateWebHostBuilder(args)
                .Build()
                .Run();
        }

        public static IConfiguration HttpsSetupConfiguration(string[] args)
            => new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables("HTTPS")
                .AddJsonFile("appsettings.json", true, true)
                .Build();

        public static IWebHostBuilder CreateAcmeHostBuilder(string[] args)
           => new WebHostBuilder()
                .UseConfiguration(HttpsSetupConfiguration(args))
                .ConfigureServices(services => {
                    var configuration = HttpsSetupConfiguration(args);

                    services.AddOptions();
                    services.Configure<LetsEncryptOptions>(configuration.GetSection(LetsEncryptOptions.SectionName));

                    services.AddAcmeChallenge();
                    services.AddSingleton<IHttpChallengeResponseStore, InMemoryHttpChallengeResponseStore>();
                    services.AddSingleton<IStartupFilter, AcmeStartupFilter>();
                    services.AddSingleton<IHostedService, AcmeService>();
                })
                .UseKestrel(options => options.ListenAnyIP(80));

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options => {
                    var configuration = HttpsSetupConfiguration(args);
                    var letsencryptOptions = configuration
                        .GetSection(LetsEncryptOptions.SectionName)
                        .Get<LetsEncryptOptions>();

                    options.ListenAnyIP(80);
                    options.ListenAnyIP(443, o => o.UseHttps(letsencryptOptions.Certificate.Filename, letsencryptOptions.Certificate.Password));
                })
                .UseStartup<Startup>();
    }
}
