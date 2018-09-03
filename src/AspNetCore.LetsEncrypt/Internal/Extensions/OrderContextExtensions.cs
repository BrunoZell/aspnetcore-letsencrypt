using Certes;
using Certes.Acme;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AspNetCore.LetsEncrypt.Internal.Extensions {
    internal static class OrderContextExtensions {
        public static async Task<X509Certificate2> GetFinalCertificate(this IOrderContext orderContext, Options.CsrInfo csrInfo, string commonName, string friendlyName)
        {
            commonName.ArgNotNull(nameof(commonName));
            friendlyName.ArgNotNull(nameof(friendlyName));

            // Download final certificate
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var certificate = await orderContext.Generate(new CsrInfo {
                CountryName = csrInfo?.CountryName,
                State = csrInfo?.State,
                Locality = csrInfo?.Locality,
                Organization = csrInfo?.Organization,
                OrganizationUnit = csrInfo?.OrganizationUnit,
                CommonName = commonName
            }, privateKey);

            // Generate pfx and then load it into a X509Certificate2 class.
            // Havent found a conversion to X509Certificate2 without the need for a password...
            string tempPassword = Guid.NewGuid().ToString();
            byte[] pfx = certificate
                .ToPfx(privateKey)
                .Build(friendlyName, tempPassword);

            return new X509Certificate2(pfx, tempPassword, X509KeyStorageFlags.Exportable);
        }
    }
}
