using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Microsoft.AspNetCore.Hosting;
using WebApp.Internal.Abstractions;
using WebApp.Options;

namespace WebApp.Internal {
    internal class AcmeChallengeRequester : HostedService {
        private readonly LetsEncryptOptions options;
        private readonly IApplicationLifetime application;
        private readonly IHttpChallengeResponseStore responseStore;

        public AcmeChallengeRequester(LetsEncryptOptions options, IApplicationLifetime application, IHostingEnvironment hostingEnvironment, IHttpChallengeResponseStore responseStore) {
            this.options = options;
            this.application = application;
            this.responseStore = responseStore;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            try {
                if (TestForValidCertificate(options.Certificate, options.RenewalBuffer, options.Authority.Name)) {
                    // A valid certificate exists. Fast forward to the web app...
                    application.StopApplication();
                    return;
                }

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
                await File.WriteAllBytesAsync(options.Certificate.Filename, cartificatePfx);
            }
            catch (Exception) {
                // Todo: Log errors and terminate web app. Also make it configurable if to terminate or not
                ;
            }
            finally {
                // Stop intermediary application and start the web app
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

        private static bool TestForValidCertificate(Certificate certificate, TimeSpan renewalBuffer, string authorityName) {
            if (!File.Exists(certificate.Filename)) {
                // Certificate does not exist yet
                return false;
            }

            // Certificate exists already
            var existingCertificates = new X509Certificate2Collection();
            existingCertificates.Import(certificate.Filename, certificate.Password, X509KeyStorageFlags.PersistKeySet);

            // Test if a certificate is issued by the specified authority and whether it's not expired
            return existingCertificates
                .Cast<X509Certificate2>()
                .Where(c => c.Issuer.Equals(authorityName, StringComparison.InvariantCultureIgnoreCase))
                .Any(c => (c.NotAfter - renewalBuffer) > DateTime.Now && c.NotBefore < DateTime.Now);
        }
    }
}
