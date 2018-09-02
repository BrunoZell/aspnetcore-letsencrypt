using System;
using AspNetCore.LetsEncrypt.Exceptions;
using AspNetCore.LetsEncrypt.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace AspNetCore.LetsEncrypt {
    public class LetsEncryptBuilder {
        private Action<LetsEncryptOptions> configureAction;
        private IConfigurationSection configurationSection;
        private Action<ErrorInfo> errorHandler;
        private Func<Certificate, IWebHost> continueHandler;

        public LetsEncryptBuilder WithOptions(Action<LetsEncryptOptions> configureAction) {
            this.configureAction = configureAction;
            return this;
        }

        public LetsEncryptBuilder WithConfiguration(IConfigurationSection configurationSection) {
            this.configurationSection = configurationSection;
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

        public void Run() {
            var letsEncrypt = BuildLetsEncrypt();

            try {
                letsEncrypt.EnsureSslCertificate();
            }
            catch (LetsEncryptException ex) {
                if (errorHandler != null) {
                    var errorInfo = new ErrorInfo {
                        Continue = continueHandler != null,
                        Exception = ex
                    };

                    errorHandler(errorInfo);

                    if (!errorInfo.Continue) {
                        continueHandler = null;
                    }
                }
            }

            continueHandler?.Invoke(letsEncrypt.Options.Certificate)?.Run();
        }

        // Todo: RunAsync

        private LetsEncrypt BuildLetsEncrypt() {
            // Todo: If both are specified, use the action to overwrite IConfigurationSection
            if (configureAction != null) {
                var options = new LetsEncryptOptions();
                configureAction(options);
                return new LetsEncrypt(options);
            }

            if (configurationSection != null) {
                return new LetsEncrypt(configurationSection);
            }

            throw new Exception($"Lets Encrypt is not configured. Configure by invoking either {nameof(LetsEncryptBuilder.WithConfiguration)}() or {nameof(LetsEncryptBuilder.WithOptions)}().");
        }
    }
}
