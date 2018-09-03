using AspNetCore.LetsEncrypt.Internal;
using AspNetCore.LetsEncrypt.Internal.Extensions;
using AspNetCore.LetsEncrypt.Options;
using AspNetCore.LetsEncrypt.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AspNetCore.LetsEncrypt {
    public class LetsEncrypt {
        public LetsEncryptOptions Options { get; internal set; }
        internal Action<IWebHostBuilder> ConfigureHandler { get; set; }
        internal ICertificateSaver CertificateSaver { get; set; }
        internal ICertificateLoader CertificateLoader { get; set; }
        internal Action<ErrorInfo> ErrorHandler { get; set; }
        internal Func<X509Certificate2, IWebHost> ContinueHandler { get; set; }

        // Todo: Logging

        public LetsEncrypt(LetsEncryptOptions options)
        {
            Options = options.ArgNotNull(nameof(options));
        }

        #region Synchronous
        public void Run()
        {
            try {
                EnsureCertificate();
            } catch (LetsEncryptException ex) {
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

            // Retrieve the certificate from loader
            X509Certificate2 certificate = null;
            CertificateLoader?.TryLoad(Options.Hostname, out certificate);

            // This starts the actual web app
            ContinueHandler
                ?.Invoke(certificate)
                ?.Run();
        }

        private void EnsureCertificate()
        {
            if (CheckForValidCertificate()) {
                return;
            }

            var errorReporter = new ErrorReporter();
            CreateAcmeWebHostBuilder(errorReporter)
                .UseExternalConfiguration(ConfigureHandler)
                .Build()
                .Run();

            errorReporter.ThrowOnError();
        }
        #endregion

        #region Asynchronuous
        public async Task RunAsync()
        {
            try {
                await EnsureCertificateAsync();
            } catch (LetsEncryptException ex) {
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

            // Retrieve the certificate from loader
            X509Certificate2 certificate = null;
            CertificateLoader?.TryLoad(Options.Hostname, out certificate);

            // This starts the actual web app
            await ContinueHandler
                ?.Invoke(certificate)
                ?.RunAsync();
        }

        private async Task EnsureCertificateAsync()
        {
            if (CheckForValidCertificate()) {
                return;
            }

            var errorReporter = new ErrorReporter();
            await CreateAcmeWebHostBuilder(errorReporter)
                .UseExternalConfiguration(ConfigureHandler)
                .Build()
                .RunAsync();

            errorReporter.ThrowOnError();
        }
        #endregion

        private IWebHostBuilder CreateAcmeWebHostBuilder(ErrorReporter errorReporter) =>
            new WebHostBuilder()
                .UseKestrel(options => options.ListenAnyIP(80))
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices(services => {
                    services.AddSingleton(Options);
                    services.AddSingleton(errorReporter);
                    services.AddSingleton(CertificateSaver);
                    services.AddSingleton(CertificateLoader);
                })
                .UseStartup<AcmeHostStartup>();

        private bool CheckForValidCertificate()
        {
            if (!CertificateLoader.TryLoad(Options.Hostname, out var certificate)) {
                // Certificate does not exist yet
                return false;
            }
            
            // Test if the certificate is issued by the specified authority and whether it's not expired
            return certificate.Issuer.Equals(Options.Authority.Name, StringComparison.InvariantCultureIgnoreCase) &&
                (certificate.NotAfter - Options.RenewalBuffer) > DateTime.Now && certificate.NotBefore < DateTime.Now;
        }
    }
}
