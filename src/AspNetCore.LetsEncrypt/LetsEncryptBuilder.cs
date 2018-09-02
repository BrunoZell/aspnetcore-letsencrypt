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
        // Todo: Validate some required options

        public LetsEncryptBuilder WithOptions(Action<LetsEncryptOptions> configureAction)
        {
            _configureAction = configureAction;
            return this;
        }

        public LetsEncryptBuilder WithConfiguration(IConfigurationSection configurationSection)
        {
            _configurationSection = configurationSection;
            return this;
        }

        public LetsEncryptBuilder ConfigureWebHost(Action<IWebHostBuilder> configureHandler)
        {
            _configureHandler = configureHandler;
            return this;
        }

        public LetsEncryptBuilder OnError(Action<ErrorInfo> errorHandler)
        {
            _errorHandler = errorHandler;
            return this;
        }

        public LetsEncryptBuilder ContinueWith(Func<Certificate, IWebHost> continueHandler)
        {
            _continueHandler = continueHandler;
            return this;
        }

        public LetsEncrypt Build()
        {
            if (_configurationSection == null && _configureAction == null) {
                throw new Exception($"Lets Encrypt is not configured. Configure by invoking either {nameof(LetsEncryptBuilder.WithConfiguration)}() or {nameof(LetsEncryptBuilder.WithOptions)}().");
            }

            var options = _configurationSection != null ?
                _configurationSection.Get<LetsEncryptOptions>() :
                new LetsEncryptOptions();

            _configureAction?.Invoke(options);

            return new LetsEncrypt(options) {
                ConfigureHandler = _configureHandler,
                ErrorHandler = _errorHandler,
                ContinueHandler = _continueHandler
            };
        }
    }
}
