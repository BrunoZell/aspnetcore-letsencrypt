using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

        public void EnsureHttps() {
            if (TestForValidCertificate(Options.Certificate, Options.RenewalBuffer, Options.Authority.Name)) {
                return;
            }

            // Todo: Inform over success (injected singleton maybe?)
            // Todo: Logging
            CreateAcmeHostBuilder()
                .Build()
                .Run();
        }

        public async Task EnsureHttpsAsync() {
            if (TestForValidCertificate(Options.Certificate, Options.RenewalBuffer, Options.Authority.Name)) {
                return;
            }

            await CreateAcmeHostBuilder()
                .Build()
                .RunAsync();
        }

        private IWebHostBuilder CreateAcmeHostBuilder() =>
            new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel(options => options.ListenAnyIP(80))
                .ConfigureServices(services => services.AddSingleton(Options))
                .UseStartup<HostStartup>();

        private static IConfiguration BuildConfiguration(string[] args) =>
            new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

        private static bool TestForValidCertificate(Certificate certificate, TimeSpan renewalBuffer, string authorityName) {
            if (!File.Exists(certificate.Filename)) {
                // Certificate does not exist yet
                return false;
            }

            // Certificate exists already
            var existingCertificates = new X509Certificate2Collection();
            existingCertificates.Import(certificate.Filename, certificate.Password, X509KeyStorageFlags.PersistKeySet);

            // Test if a certificate is issued by the specified authority and whether it's not expired
            return existingCertificates
                .Cast<X509Certificate2>()
                .Where(c => c.Issuer.Equals(authorityName, StringComparison.InvariantCultureIgnoreCase))
                .Any(c => ( c.NotAfter - renewalBuffer ) > DateTime.Now && c.NotBefore < DateTime.Now);
        }
    }
}
