using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
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
                if (TestForValidCertificate(options.Certificate, options.Authority.Name)) {
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

                // Request validation http request
                await httpChallenge.Validate();

                // Get the ressource to check if it's valid
                var challengeRessource = await httpChallenge.Resource();
                if (challengeRessource.Status != Certes.Acme.Resource.ChallengeStatus.Valid) {
                    ;
                    // Todo: Wait until challenge has finished
                }

                // Download final certificate
                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var certificate = await order.Generate(new Certes.CsrInfo {
                    CountryName = options.CsrInfo.CountryName,
                    State = options.CsrInfo.State,
                    Locality = options.CsrInfo.Locality,
                    Organization = options.CsrInfo.Organization,
                    OrganizationUnit = options.CsrInfo.OrganizationUnit,
                    CommonName = options.Hostname
                }, privateKey);

                // Generate the pfx for file storage
                byte[] cartificatePfx = certificate
                    .ToPfx(privateKey)
                    .Build(options.Certificate.FriendlyName, options.Certificate.Password);

                // Write pfx to file
                await File.WriteAllBytesAsync(options.Certificate.Filename, cartificatePfx);
            }
            catch (Exception) {
                ;
            } finally {
                // Stop application and start the web app
                application.StopApplication();
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

        private static bool TestForValidCertificate(Certificate certificate, string authorityName) {
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
                .Any(c => c.NotAfter > DateTime.Now && c.NotBefore < DateTime.Now);
        }
    }
}
