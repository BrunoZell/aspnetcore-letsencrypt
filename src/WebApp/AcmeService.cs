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

namespace WebApp {
    public class AcmeService : HostedService {
        private readonly IApplicationLifetime applicationLifetime;
        private readonly IMemoryCache memoryCache;
        private readonly IHostingEnvironment hostingEnvironment;

        public AcmeService(IApplicationLifetime applicationLifetime, IMemoryCache memoryCache, IHostingEnvironment hostingEnvironment) {
            this.applicationLifetime = applicationLifetime;
            this.memoryCache = memoryCache;
            this.hostingEnvironment = hostingEnvironment;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            var pfxPath = Path.Combine(hostingEnvironment.ContentRootPath, "https-cert.pfx");
            try {

                if (File.Exists(pfxPath)) {
                    // certificate exists already. Validate and renew if neccessary
                    var collection = new X509Certificate2Collection();
                    collection.Import(pfxPath, "abcd1234", X509KeyStorageFlags.PersistKeySet);

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

                var acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
                var account = await acme.NewAccount("bruno.zzell@gmail.com", true);

                // Save the account key for later use
                var pemKey = acme.AccountKey.ToPem();

                var order = await acme.NewOrder(new[] { "02041c1c.ngrok.io" });

                var authz = ( await order.Authorizations() ).First();
                var httpChallenge = await authz.Http();
                var keyAuthz = httpChallenge.KeyAuthz;

                memoryCache.Set("token", httpChallenge.Token);
                memoryCache.Set("response", keyAuthz);

                var x = await httpChallenge.Validate();

                await Task.Delay(TimeSpan.FromSeconds(1));

                var challenge = await authz.Http();
                var challengeRessource = await challenge.Resource();

                var challengeResult = await challenge.Validate();


                x = await httpChallenge.Validate();
                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var cert = await order.Generate(new CsrInfo {
                    CountryName = "DE",
                    State = "Ontario",
                    Locality = "Toronto",
                    Organization = "Certes",
                    OrganizationUnit = "Dev",
                    CommonName = "02041c1c.ngrok.io",
                }, privateKey);

                var certPem = cert.ToPem();

                // OR

                var pfxBuilder = cert.ToPfx(privateKey);
                var pfx = pfxBuilder.Build("https-cert", "abcd1234");
                await File.WriteAllBytesAsync(pfxPath, pfx);

                applicationLifetime.StopApplication();
            }
            catch (Exception ex) {

                throw;
            }
        }
    }
}
