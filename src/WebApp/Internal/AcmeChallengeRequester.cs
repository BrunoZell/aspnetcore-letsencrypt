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


                IAccountContext account;
                IAcmeContext acme;
                var directoryUri = new Uri(options.Authority.DirectoryUri);

                if (!string.IsNullOrWhiteSpace(options.AccountKey)) {
                    // Load the saved account key
                    var accountKey = KeyFactory.FromPem(options.AccountKey);
                    acme = new AcmeContext(directoryUri, accountKey);
                    account = await acme.Account();

                }
                else {
                    // Create new account
                    acme = new AcmeContext(directoryUri);
                    account = await acme.NewAccount(options.Email, true);
                    // Todo: Save the account key for later use
                    options.AccountKey = acme.AccountKey.ToPem();
                }

                var order = await acme.NewOrder(new[] { options.Hostname });

                var authz = ( await order.Authorizations() ).First();
                var httpChallenge = await authz.Http();
                string keyAuthz = httpChallenge.KeyAuthz;

                responseStore.AddChallengeResponse(httpChallenge.Token, keyAuthz);

                var x = await httpChallenge.Validate();

                await Task.Delay(TimeSpan.FromSeconds(1));

                var challenge = await authz.Http();
                var challengeRessource = await challenge.Resource();

                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var cert = await order.Generate(new Certes.CsrInfo {
                    CountryName = options.CsrInfo.CountryName,
                    State = options.CsrInfo.State,
                    Locality = options.CsrInfo.Locality,
                    Organization = options.CsrInfo.Organization,
                    OrganizationUnit = options.CsrInfo.OrganizationUnit,
                    CommonName = options.Hostname
                }, privateKey);

                var pfxBuilder = cert.ToPfx(privateKey);
                byte[] pfx = pfxBuilder.Build(options.Certificate.FriendlyName, options.Certificate.Password);
                await File.WriteAllBytesAsync(options.Certificate.Filename, pfx);

                application.StopApplication();
            }
            catch (Exception) {

                throw;
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
