using System;
using AspNetCore.LetsEncrypt.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace AspNetCore.LetsEncrypt {
    public class LetsEncryptBuilder {
        private Action<LetsEncryptOptions> configureAction;
        private IConfigurationSection configurationSection;
        private Action<IWebHostBuilder> configureHandler;
        private Action<ErrorInfo> errorHandler;
        private Func<Certificate, IWebHost> continueHandler;

        // Todo: Pass (user defined) certificate store
        // Todo: Pass (user defined) auth-key store
        // Todo: Validate some required options

        public LetsEncryptBuilder WithOptions(Action<LetsEncryptOptions> configureAction) {
            this.configureAction = configureAction;
            return this;
        }

        public LetsEncryptBuilder WithConfiguration(IConfigurationSection configurationSection) {
            this.configurationSection = configurationSection;
            return this;
        }

        public LetsEncryptBuilder ConfigureWebHost(Action<IWebHostBuilder> configureHandler) {
            this.configureHandler = configureHandler;
            return this;
        }

        public LetsEncryptBuilder OnError(Action<ErrorInfo> errorHandler) {
            this.errorHandler = errorHandler;
            return this;
        }

        public LetsEncryptBuilder ContinueWith(Func<Certificate, IWebHost> continueHandler) {
            this.continueHandler = continueHandler;
            return this;
        }

        public LetsEncrypt Build() {
            if (configurationSection == null && configureAction == null) {
                throw new Exception($"Lets Encrypt is not configured. Configure by invoking either {nameof(LetsEncryptBuilder.WithConfiguration)}() or {nameof(LetsEncryptBuilder.WithOptions)}().");
            }

            var options = configurationSection != null ?
                configurationSection.Get<LetsEncryptOptions>() :
                new LetsEncryptOptions();

            configureAction?.Invoke(options);

            return new LetsEncrypt(options) {
                ConfigureHandler = configureHandler,
                ErrorHandler = errorHandler,
                ContinueHandler = continueHandler
            };
        }
    }
}
