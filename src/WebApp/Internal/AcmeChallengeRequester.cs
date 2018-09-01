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
        private readonly IApplicationLifetime applicationLifetime;
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly IHttpChallengeResponseStore responseStore;

        public AcmeChallengeRequester(LetsEncryptOptions options, IApplicationLifetime applicationLifetime, IHostingEnvironment hostingEnvironment, IHttpChallengeResponseStore responseStore) {
            this.options = options;
            this.applicationLifetime = applicationLifetime;
            this.hostingEnvironment = hostingEnvironment;
            this.responseStore = responseStore;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            try {

                if (File.Exists(options.Certificate.Filename)) {
                    // certificate exists already. Validate and renew if neccessary
                    var collection = new X509Certificate2Collection();
                    collection.Import(options.Certificate.Filename, options.Certificate.Password, X509KeyStorageFlags.PersistKeySet);

                    foreach (var certificate in collection.Cast<X509Certificate2>().Where(c => c.Issuer.Equals("CN=Fake LE Root X1", StringComparison.InvariantCultureIgnoreCase))) {
                        if (certificate.NotAfter > DateTime.Now && certificate.NotBefore < DateTime.Now) {
                            // Still valid
                            applicationLifetime.StopApplication();
                            return;
                        }
                    }

                    // Expired. Renew...

                    return;
                }

                IAccountContext account;
                IAcmeContext acme;

                if (!string.IsNullOrWhiteSpace(options.AccountKey)) {
                    // Load the saved account key
                    var accountKey = KeyFactory.FromPem(options.AccountKey);
                    acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2, accountKey);
                    account = await acme.Account();

                }
                else {
                    // Create new account
                    acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
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

                applicationLifetime.StopApplication();
            }
            catch (Exception) {

                throw;
            }
        }
    }
}
