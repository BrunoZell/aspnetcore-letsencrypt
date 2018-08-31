using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WebApp.Options;

namespace WebApp {
    public class AcmeService : HostedService {
        private readonly LetsEncryptOptions options;
        private readonly IApplicationLifetime applicationLifetime;
        private readonly IMemoryCache memoryCache;
        private readonly IHostingEnvironment hostingEnvironment;

        public AcmeService(IOptions<LetsEncryptOptions> options, IApplicationLifetime applicationLifetime, IMemoryCache memoryCache, IHostingEnvironment hostingEnvironment) {
            this.options = options.Value;
            this.applicationLifetime = applicationLifetime;
            this.memoryCache = memoryCache;
            this.hostingEnvironment = hostingEnvironment;
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
                var keyAuthz = httpChallenge.KeyAuthz;

                memoryCache.Set("token", httpChallenge.Token);
                memoryCache.Set("response", keyAuthz);

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
                    CommonName = options.CsrInfo.CommonName
                }, privateKey);

                var pfxBuilder = cert.ToPfx(privateKey);
                var pfx = pfxBuilder.Build(options.Certificate.FriendlyName, options.Certificate.Password);
                await File.WriteAllBytesAsync(options.Certificate.Filename, pfx);

                applicationLifetime.StopApplication();
            }
            catch (Exception ex) {

                throw;
            }
        }
    }
}
