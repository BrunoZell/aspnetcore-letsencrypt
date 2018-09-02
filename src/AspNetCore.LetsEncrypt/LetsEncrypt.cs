using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using AspNetCore.LetsEncrypt.Exceptions;
using AspNetCore.LetsEncrypt.Extensions;
using AspNetCore.LetsEncrypt.Internal;
using AspNetCore.LetsEncrypt.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCore.LetsEncrypt {
    public class LetsEncrypt {
        public LetsEncryptOptions Options { get; internal set; }
        internal Action<IWebHostBuilder> ConfigureHandler { get; set; }
        internal Action<ErrorInfo> ErrorHandler { get; set; }
        internal Func<Certificate, IWebHost> ContinueHandler { get; set; }

        // Todo: Logging

        public LetsEncrypt(LetsEncryptOptions options) {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Run() {
            try {
                EnsureCertificate();
            }
            catch (LetsEncryptException ex) {
                if (ErrorHandler != null) {
                    var errorInfo = new ErrorInfo {
                        Continue = ContinueHandler != null,
                        Exception = ex
                    };

                    ErrorHandler(errorInfo);

                    if (!errorInfo.Continue) {
                        ContinueHandler = null;
                    }
                }
            }

            // This starts the actual web app
            ContinueHandler
                ?.Invoke(Options.Certificate)
                ?.Run();
        }

        private void EnsureCertificate() {
            if (CheckForValidCertificate()) {
                return;
            }

            var errorReporter = new ErrorReporter();
            CreateAcmeHostBuilder(Options, errorReporter)
                .UseExternalConfiguration(ConfigureHandler)
                .Build()
                .Run();

            errorReporter.ThrowOnError();
        }

        private static IWebHostBuilder CreateAcmeHostBuilder(LetsEncryptOptions options, ErrorReporter errorReporter) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices(services => {
                    services.AddSingleton(options);
                    services.AddSingleton(errorReporter);
                })
                .UseStartup<HostStartup>();

        private bool CheckForValidCertificate() {
            if (!File.Exists(Options.Certificate.Filename)) {
                // Certificate does not exist yet
                return false;
            }

            // Certificate exists already
            var existingCertificates = new X509Certificate2Collection();
            existingCertificates.Import(Options.Certificate.Filename, Options.Certificate.Password, X509KeyStorageFlags.PersistKeySet);

            // Test if a certificate is issued by the specified authority and whether it's not expired
            return existingCertificates
                .Cast<X509Certificate2>()
                .Where(c => c.Issuer.Equals(Options.Authority.Name, StringComparison.InvariantCultureIgnoreCase))
                .Any(c => ( c.NotAfter - Options.RenewalBuffer ) > DateTime.Now && c.NotBefore < DateTime.Now);
            // Todo: edit .editorconfig
        }
    }
}
