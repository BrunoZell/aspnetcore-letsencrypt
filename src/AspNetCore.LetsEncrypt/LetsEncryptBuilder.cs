using AspNetCore.LetsEncrypt.Exceptions;
using AspNetCore.LetsEncrypt.Internal.Extensions;
using AspNetCore.LetsEncrypt.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace AspNetCore.LetsEncrypt {
    public class LetsEncryptBuilder {
        private Action<LetsEncryptOptions> _configureAction;
        private IConfigurationSection _configurationSection;
        private Action<IWebHostBuilder> _configureHandler;
        private Action<ErrorInfo> _errorHandler;
        private Func<Certificate, IWebHost> _continueHandler;

        // Todo: Pass (user defined) certificate store
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

        public LetsEncryptBuilder ConfigureWebHost(Action<IWebHostBuilder> configureHandler)
        {
            _configureHandler = configureHandler.ArgNotNull(nameof(configureHandler));
            return this;
        }

        public LetsEncryptBuilder OnError(Action<ErrorInfo> errorHandler)
        {
            _errorHandler = errorHandler.ArgNotNull(nameof(errorHandler));
            return this;
        }

        public LetsEncryptBuilder ContinueWith(Func<Certificate, IWebHost> continueHandler)
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
                ContinueHandler = _continueHandler
            };
        }

        private static void ValidateOptions(LetsEncryptOptions options)
        {
            options.ArgNotNull(nameof(options));

            // Validate options
            options.Hostname.OptionNotBlank(nameof(LetsEncryptOptions.Hostname));
            options.Email.OptionNotBlank(nameof(LetsEncryptOptions.Email));

            // Validate authority options
            options.Authority?.Name.OptionNotBlank($"{nameof(LetsEncryptOptions.Authority)}.{nameof(Authority.Name)}");
            options.Authority?.DirectoryUri.OptionNotBlank($"{nameof(LetsEncryptOptions.Authority)}.{nameof(Authority.DirectoryUri)}");

            // Validate certificate options
            options.Certificate?.Filename.OptionNotBlank($"{nameof(LetsEncryptOptions.Certificate)}.{nameof(Certificate.Filename)}");
            options.Certificate?.FriendlyName.OptionNotNull($"{nameof(LetsEncryptOptions.Certificate)}.{nameof(Certificate.FriendlyName)}");
            options.Certificate?.Password.OptionNotNull($"{nameof(LetsEncryptOptions.Certificate)}.{nameof(Certificate.Password)}");

            // Validate certificate signing request (CSR) options
            options.CsrInfo?.CountryName.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.CountryName)}");
            options.CsrInfo?.State.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.State)}");
            options.CsrInfo?.Locality.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.Locality)}");
            options.CsrInfo?.Organization.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.Organization)}");
            options.CsrInfo?.OrganizationUnit.OptionNotNull($"{nameof(LetsEncryptOptions.CsrInfo)}.{nameof(CsrInfo.OrganizationUnit)}");
        }
    }
}
