using AspNetCore.LetsEncrypt.Internal.Extensions;
using AspNetCore.LetsEncrypt.Options;
using AspNetCore.LetsEncrypt.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography.X509Certificates;

namespace AspNetCore.LetsEncrypt {
    public class LetsEncryptBuilder {
        private Action<LetsEncryptOptions> _configureAction;
        private IConfigurationSection _configurationSection;
        private ICertificateSaver _certificateSaver;
        private ICertificateLoader _certificateLoader;
        private Action<IWebHostBuilder> _configureHandler;
        private Action<ErrorInfo> _errorHandler;
        private Func<X509Certificate2, IWebHost> _continueHandler;

        // Todo: Pass (user defined) auth-key store

        public LetsEncryptBuilder WithOptions(Action<LetsEncryptOptions> configureAction)
        {
            _configureAction = configureAction.ArgNotNull(nameof(configureAction));
            return this;
        }

        public LetsEncryptBuilder WithConfiguration(IConfigurationSection configurationSection)
        {
            _configurationSection = configurationSection.ArgNotNull(nameof(configurationSection));
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

        public LetsEncryptBuilder ConfigureWebHost(Action<IWebHostBuilder> configureHandler)
        {
            // Todo: Use IHostingStartup instead
            _configureHandler = configureHandler.ArgNotNull(nameof(configureHandler));
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
            if (_configurationSection == null && _configureAction == null) {
                throw new LetsEncryptException($"Lets Encrypt is not configured. Configure by invoking either {nameof(LetsEncryptBuilder.WithConfiguration)}() or {nameof(LetsEncryptBuilder.WithOptions)}().");
            }

            // Get options by (1) parse the passed configuration, if any...
            var options = _configurationSection != null ?
                _configurationSection.Get<LetsEncryptOptions>() :
                new LetsEncryptOptions();

            // ... and (2) overwrite some options with the configure delegate, if any.
            _configureAction?.Invoke(options);

            ValidateOptions(options);

            return new LetsEncrypt(options) {
                ConfigureHandler = _configureHandler,
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
