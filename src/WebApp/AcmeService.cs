using System;
using System.Linq;
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

        public AcmeService(IApplicationLifetime applicationLifetime, IMemoryCache memoryCache) {
            this.applicationLifetime = applicationLifetime;
            this.memoryCache = memoryCache;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            try {
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
                var pfx = pfxBuilder.Build("my-cert", "abcd1234");

                applicationLifetime.StopApplication();
            }
            catch (Exception ex) {

                throw;
            }
        }
    }
}
