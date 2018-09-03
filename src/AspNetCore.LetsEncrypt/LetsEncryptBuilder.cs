using AspNetCore.LetsEncrypt.Internal.Extensions;
using AspNetCore.LetsEncrypt.Options;
using AspNetCore.LetsEncrypt.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography.X509Certificates;

namespace AspNetCore.LetsEncrypt {
    public class LetsEncryptBuilder {
        private IConfigurationSection _configurationSection;
        private Action<LetsEncryptOptions> _configureOptionsHandler;
        private Action<IWebHostBuilder> _configureWebHostHandler;
        private ICertificateSaver _certificateSaver;
        private ICertificateLoader _certificateLoader;
        private Action<ErrorInfo> _errorHandler;
        private Func<X509Certificate2, IWebHost> _continueHandler;

        // Todo: Pass (user defined) auth-key store

        public LetsEncryptBuilder WithConfiguration(IConfigurationSection configurationSection)
        {
            _configurationSection = configurationSection.ArgNotNull(nameof(configurationSection));
            return this;
        }

        public LetsEncryptBuilder WithOptions(Action<LetsEncryptOptions> configureOptionsHandler)
        {
            _configureOptionsHandler = configureOptionsHandler.ArgNotNull(nameof(configureOptionsHandler));
            return this;
        }

        public LetsEncryptBuilder ConfigureWebHost(Action<IWebHostBuilder> configureWebHostHandler)
        {
            _configureWebHostHandler = configureWebHostHandler.ArgNotNull(nameof(configureWebHostHandler));
            return this;
        }

        public LetsEncryptBuilder UseCertificateSaver(ICertificateSaver certificateSaver)
        {
            _certificateSaver = certificateSaver.ArgNotNull(nameof(certificateSaver));
            return this;
        }

        public LetsEncryptBuilder UseCertificateLoader(ICertificateLoader certificateLoader)
        {
            _certificateLoader = certificateLoader.ArgNotNull(nameof(certificateLoader));
            return this;
        }

        public LetsEncryptBuilder OnError(Action<ErrorInfo> errorHandler)
        {
            _errorHandler = errorHandler.ArgNotNull(nameof(errorHandler));
            return this;
        }

        public LetsEncryptBuilder ContinueWith(Func<X509Certificate2, IWebHost> continueHandler)
        {
            _continueHandler = continueHandler.ArgNotNull(nameof(continueHandler));
            return this;
        }

        public LetsEncrypt Build()
        {
            if (_configurationSection == null && _configureOptionsHandler == null) {
                throw new LetsEncryptException($"Lets Encrypt is not configured. " +
                    $"Configure by invoking either {nameof(WithConfiguration)}() " +
                    $"or {nameof(WithOptions)}() on {nameof(LetsEncryptBuilder)}.");
            }

            if (_certificateLoader == null) {
                throw new LetsEncryptException($"No certificate loader is configured. Certificate loaders " +
                    $"implement {nameof(ICertificateLoader)} and are used to read already existing certificates. Reference one by " +
                    $"invoking {nameof(UseCertificateLoader)}() on {nameof(LetsEncryptBuilder)}.");
            }

            if (_certificateSaver == null) {
                throw new LetsEncryptException($"No certificate saver is configured. Certificate savers " +
                    $"implement {nameof(ICertificateSaver)} and are used to store the acuired certificate for " +
                    $"later use. Reference one by invoking {nameof(UseCertificateSaver)}() on {nameof(LetsEncryptBuilder)}.");
            }

            // Get options by (1) parse the passed configuration, if any...
            var options = _configurationSection != null ?
                _configurationSection.Get<LetsEncryptOptions>() :
                new LetsEncryptOptions();

            // ... and (2) overwrite some options with the configure delegate, if any.
            _configureOptionsHandler?.Invoke(options);

            ValidateOptions(options);

            return new LetsEncrypt(options) {
                ConfigureWebHostHandler = _configureWebHostHandler,
                ErrorHandler = _errorHandler,
                ContinueHandler = _continueHandler,
                CertificateLoader = _certificateLoader,
                CertificateSaver = _certificateSaver
            };
        }

        private static void ValidateOptions(LetsEncryptOptions options)
        {
            options.ArgNotNull(nameof(options));

            // Validate options
            options.Email.OptionNotBlank(nameof(LetsEncryptOptions.Email));
            options.Hostname.OptionNotBlank(nameof(LetsEncryptOptions.Hostname));
            options.FriendlyName.OptionNotBlank(nameof(LetsEncryptOptions.FriendlyName));

            // Validate authority options
            options.Authority?.Name.OptionNotBlank($"{nameof(LetsEncryptOptions.Authority)}.{nameof(Authority.Name)}");
            options.Authority?.DirectoryUri.OptionNotBlank($"{nameof(LetsEncryptOptions.Authority)}.{nameof(Authority.DirectoryUri)}");

            // Validate certificate signing request (CSR) options
            options.CsrInfo?.CountryName.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.CountryName)}");
            options.CsrInfo?.State.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.State)}");
            options.CsrInfo?.Locality.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.Locality)}");
            options.CsrInfo?.Organization.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.Organization)}");
            options.CsrInfo?.OrganizationUnit.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.OrganizationUnit)}");
        }
    }
}
