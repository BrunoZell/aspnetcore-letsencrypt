using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApp.Internal;
using WebApp.Options;

namespace WebApp {
    public class LetsEncrypt {
        public LetsEncryptOptions Options { get; }

        public LetsEncrypt(string[] args)
            : this(BuildConfiguration(args).GetSection("LetsEncrypt")) { }

        public LetsEncrypt(IConfigurationSection configurationSection)
            : this(configurationSection?.Get<LetsEncryptOptions>()) { }

        public LetsEncrypt(LetsEncryptOptions options) {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            // Todo: Validate some required options
        }

        public void EnsureHttps() =>
            CreateAcmeHostBuilder()
                .Build()
                .Run();

        public async Task EnsureHttpsAsync() =>
            await CreateAcmeHostBuilder()
                .Build()
                .RunAsync();

        private IWebHostBuilder CreateAcmeHostBuilder() =>
            new WebHostBuilder()
                .ConfigureServices(services => services.AddSingleton(Options))
                .UseKestrel(options => options.ListenAnyIP(80))
                .UseStartup<HostStartup>();

        private static IConfiguration BuildConfiguration(string[] args) =>
            new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
    }
}
