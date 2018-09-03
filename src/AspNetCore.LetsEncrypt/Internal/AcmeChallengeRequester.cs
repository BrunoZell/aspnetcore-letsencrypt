﻿using AspNetCore.LetsEncrypt.Internal.Abstractions;
using AspNetCore.LetsEncrypt.Internal.Extensions;
using AspNetCore.LetsEncrypt.Options;
using AspNetCore.LetsEncrypt.Persistence;
using Certes;
using Certes.Acme;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.LetsEncrypt.Internal {
    internal class AcmeChallengeRequester : HostedService {
        private readonly LetsEncryptOptions _options;
        private readonly IApplicationLifetime _application;
        private readonly IHttpChallengeResponseStore _responseStore;
        private readonly ICertificateSaver _certificateSaver;
        private readonly ErrorReporter _errorReporter;

        public AcmeChallengeRequester(
            LetsEncryptOptions options,
            IApplicationLifetime application,
            IHttpChallengeResponseStore responseStore,
            ICertificateSaver certificateSaver,
            ErrorReporter errorReporter)
        {
            _options = options;
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
                _options.AccountKey = acme.AccountKey.ToPem();

                var order = await acme.NewOrder(new[] { _options.Hostname });
                var authorization = (await order.Authorizations()).First();
                var httpChallenge = await authorization.Http();

                // Save the expected response
                _responseStore.AddChallengeResponse(httpChallenge.Token, httpChallenge.KeyAuthz);

                // Execute http challenge
                await httpChallenge.Validate();
                await httpChallenge.WaitForCompletion(TimeSpan.FromSeconds(1));

                // Download certificate, generate pfx and write to file
                var certificate = await order.GetFinalCertificate(
                    _options.CsrInfo,
                    _options.Hostname,
                    _options.FriendlyName);

                _certificateSaver.Save(certificate, _options.Hostname);
            } catch (Exception ex) {
                _errorReporter.ReportException(ex);
            } finally {
                // Stop intermediary application
                _application.StopApplication();
            }
        }

        private static async Task<(IAcmeContext acme, IAccountContext account)> InitializeAccount(Uri directoryUri, string email, string existingAccountKey = null)
        {
            if (directoryUri == null) {
                throw new ArgumentNullException(nameof(directoryUri));
            }

            if (!String.IsNullOrWhiteSpace(existingAccountKey)) {
                // Use the existing account
                var accountKey = KeyFactory.FromPem(existingAccountKey);
                var acme = new AcmeContext(directoryUri, accountKey);
                var account = await acme.Account();
                return (acme, account);
            } else {
                if (email == null) {
                    throw new ArgumentNullException(nameof(email));
                }

                // Create new account
                var acme = new AcmeContext(directoryUri);
                var account = await acme.NewAccount(email, true);
                return (acme, account);
            }
        }
    }
}
