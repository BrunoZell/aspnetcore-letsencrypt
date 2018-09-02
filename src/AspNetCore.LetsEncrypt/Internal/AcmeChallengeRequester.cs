using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.LetsEncrypt.Internal.Abstractions;
using AspNetCore.LetsEncrypt.Options;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Microsoft.AspNetCore.Hosting;

namespace AspNetCore.LetsEncrypt.Internal {
    internal class AcmeChallengeRequester : HostedService {
        private readonly LetsEncryptOptions options;
        private readonly IApplicationLifetime application;
        private readonly IHttpChallengeResponseStore responseStore;
        private readonly ErrorReporter errorReporter;

        public AcmeChallengeRequester(LetsEncryptOptions options, IApplicationLifetime application, IHttpChallengeResponseStore responseStore, ErrorReporter errorReporter) {
            this.options = options;
            this.application = application;
            this.responseStore = responseStore;
            this.errorReporter = errorReporter;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            try {
                var directoryUri = new Uri(options.Authority.DirectoryUri);
                var (acme, account) = await InitializeAccount(directoryUri, options.Email, options.AccountKey);

                // Todo: Save the account key for later use
                options.AccountKey = acme.AccountKey.ToPem();

                var order = await acme.NewOrder(new[] { options.Hostname });
                var authorization = ( await order.Authorizations() ).First();
                var httpChallenge = await authorization.Http();

                // Save the expected response
                responseStore.AddChallengeResponse(httpChallenge.Token, httpChallenge.KeyAuthz);

                // Execute http challenge
                await httpChallenge.Validate();
                await WaitForHttpChallenge(httpChallenge);

                // Write pfx to file
                byte[] cartificatePfx = await GetFinalCertificateAsPfx(order, options.CsrInfo, options.Certificate, options.Hostname);
#if NETCOREAPP
                await File.WriteAllBytesAsync(options.Certificate.Filename, cartificatePfx);
#else
                File.WriteAllBytes(options.Certificate.Filename, cartificatePfx);
#endif
            }
            catch (Exception ex) {
                errorReporter.ReportException(ex);
            }
            finally {
                // Stop intermediary application
                application.StopApplication();
            }
        }

        // Todo: As extension method
        private static async Task<byte[]> GetFinalCertificateAsPfx(IOrderContext order, Options.CsrInfo csrInfo, Certificate certificateInfo, string hostname) {
            // Download final certificate
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var certificate = await order.Generate(new Certes.CsrInfo {
                CountryName = csrInfo.CountryName,
                State = csrInfo.State,
                Locality = csrInfo.Locality,
                Organization = csrInfo.Organization,
                OrganizationUnit = csrInfo.OrganizationUnit,
                CommonName = hostname
            }, privateKey);

            // Generate the pfx for file storage
            return certificate
                .ToPfx(privateKey)
                .Build(certificateInfo.FriendlyName, certificateInfo.Password);
        }

        // Todo: As extension method
        private static async Task WaitForHttpChallenge(IChallengeContext context) {
            // Get the challenges ressource to check if it's valid
            var challenge = await context.Resource();
            while (challenge.Status == ChallengeStatus.Pending || challenge.Status == ChallengeStatus.Processing) {
                // If nor finished processing, poll every second
                challenge = await context.Resource();
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            if (challenge.Status != ChallengeStatus.Valid) {
                // Throw if invalid
                new Exception(challenge.Error?.Detail ?? "ACME challenge not successful.");
            }
        }

        private static async Task<(IAcmeContext acme, IAccountContext account)> InitializeAccount(Uri directoryUri, string email, string existingAccountKey = null) {
            if (directoryUri == null) {
                throw new ArgumentNullException(nameof(directoryUri));
            }

            if (!string.IsNullOrWhiteSpace(existingAccountKey)) {
                // Use the existing account
                var accountKey = KeyFactory.FromPem(existingAccountKey);
                var acme = new AcmeContext(directoryUri, accountKey);
                var account = await acme.Account();
                return (acme, account);
            }
            else {
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
