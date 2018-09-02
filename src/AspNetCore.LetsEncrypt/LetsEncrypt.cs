﻿using AspNetCore.LetsEncrypt.Internal;
using AspNetCore.LetsEncrypt.Internal.Extensions;
using AspNetCore.LetsEncrypt.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AspNetCore.LetsEncrypt {
    public class LetsEncrypt {
        public LetsEncryptOptions Options { get; internal set; }
        internal Action<IWebHostBuilder> ConfigureHandler { get; set; }
        internal Action<ErrorInfo> ErrorHandler { get; set; }
        internal Func<Certificate, IWebHost> ContinueHandler { get; set; }

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

            // This starts the actual web app
            ContinueHandler
                ?.Invoke(Options.Certificate)
                ?.Run();
        }

        private void EnsureCertificate()
        {
            if (CheckForValidCertificate()) {
                return;
            }

            var errorReporter = new ErrorReporter();
            CreateAcmeWebHostBuilder(Options, errorReporter)
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

            // This starts the actual web app
            await ContinueHandler
                ?.Invoke(Options.Certificate)
                ?.RunAsync();
        }

        private async Task EnsureCertificateAsync()
        {
            if (CheckForValidCertificate()) {
                return;
            }

            var errorReporter = new ErrorReporter();
            await CreateAcmeWebHostBuilder(Options, errorReporter)
                .UseExternalConfiguration(ConfigureHandler)
                .Build()
                .RunAsync();

            errorReporter.ThrowOnError();
        }
        #endregion

        private static IWebHostBuilder CreateAcmeWebHostBuilder(LetsEncryptOptions options, ErrorReporter errorReporter) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices(services => {
                    services.AddSingleton(options);
                    services.AddSingleton(errorReporter);
                })
                .UseStartup<HostStartup>();

        private bool CheckForValidCertificate()
        {
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
                .Any(c => (c.NotAfter - Options.RenewalBuffer) > DateTime.Now && c.NotBefore < DateTime.Now);
        }
    }
}
