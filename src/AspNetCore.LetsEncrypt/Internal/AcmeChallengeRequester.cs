using AspNetCore.LetsEncrypt.Internal.Abstractions;
using AspNetCore.LetsEncrypt.Internal.Extensions;
using AspNetCore.LetsEncrypt.Options;
using AspNetCore.LetsEncrypt.Persistence;
using Certes;
using Certes.Acme;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.LetsEncrypt.Internal {
    internal class AcmeChallengeRequester : HostedService {
        private readonly LetsEncryptOptions _options;
        private readonly ILogger _logger;
        private readonly IApplicationLifetime _application;
        private readonly IHttpChallengeResponseStore _responseStore;
        private readonly ICertificateSaver _certificateSaver;
        private readonly ErrorReporter _errorReporter;

        public AcmeChallengeRequester(
            LetsEncryptOptions options,
            IEnumerable<ILogger> loggers,
            IApplicationLifetime application,
            IHttpChallengeResponseStore responseStore,
            ICertificateSaver certificateSaver,
            ErrorReporter errorReporter)
        {
            _options = options;
            _logger = loggers.FirstOrDefault();
            _application = application;
            _responseStore = responseStore;
            _certificateSaver = certificateSaver;
            _errorReporter = errorReporter;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try {
                var directoryUri = new Uri(_options.Authority.DirectoryUri);
                var (acme, account) = await InitializeAccount(directoryUri, _options.Email, _options.AccountKey);

                // Todo: Save the account key for later use
                _logger?.LogWarning("Account key is not yet persisted for later use.");
                _options.AccountKey = acme.AccountKey.ToPem();

                _logger?.LogDebug("Create new ACME order with http challenge for hostname {hostname}", _options.Hostname);
                var order = await acme.NewOrder(new[] { _options.Hostname });
                var authorization = (await order.Authorizations()).First();
                var httpChallenge = await authorization.Http();

                // Save the expected response
                _responseStore.AddChallengeResponse(httpChallenge.Token, httpChallenge.KeyAuthz);
                _logger?.LogInformation("ACME http challenge initialized and tokens cached. About to execute http challenge.");

                // Execute http challenge
                await httpChallenge.Validate();
                await httpChallenge.WaitForCompletion(TimeSpan.FromSeconds(1), _logger);

                // Download certificate, generate pfx and write to file
                var certificate = await order.GetFinalCertificate(
                    _options.CsrInfo,
                    _options.Hostname,
                    _options.FriendlyName);

                _logger?.LogInformation("Certificate successfully downloaded and is about to get persisted.");
                _certificateSaver.Save(certificate, _options.Hostname);
            } catch (Exception ex) {
                _logger?.LogError(ex, "Error while running the ACME http challenge protocol procedure.");
                _errorReporter.ReportException(ex);
            } finally {
                // Stop intermediary application
                _logger?.LogInformation("Intermediary server for ACME challenge is shutting down...");
                _application.StopApplication();
            }
        }

        private async Task<(IAcmeContext acme, IAccountContext account)> InitializeAccount(Uri directoryUri, string email, string existingAccountKey = null)
        {
            _logger?.LogDebug("Using directory uri {directoryUri}", directoryUri);
            if (!String.IsNullOrWhiteSpace(existingAccountKey)) {
                // Use the existing account
                _logger?.LogInformation("Using existing account key");
                var accountKey = KeyFactory.FromPem(existingAccountKey);
                var acme = new AcmeContext(directoryUri, accountKey);
                var account = await acme.Account();
                return (acme, account);
            } else {
                // Create new account
                _logger?.LogInformation("Creating new account with email {email}", email);
                var acme = new AcmeContext(directoryUri);
                var account = await acme.NewAccount(email, true);
                return (acme, account);
            }
        }
    }
}
