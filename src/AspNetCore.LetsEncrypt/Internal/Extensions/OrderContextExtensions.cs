using Certes;
using Certes.Acme;
using System.Threading.Tasks;

namespace AspNetCore.LetsEncrypt.Internal.Extensions {
    internal static class OrderContextExtensions {
        public static async Task<byte[]> GetFinalCertificateAsPfx(this IOrderContext orderContext, Options.CsrInfo csrInfo, string hostname, string friendlyName, string password)
        {
            csrInfo.ArgNotNull(nameof(csrInfo));
            hostname.ArgNotNull(nameof(hostname));
            friendlyName.ArgNotNull(nameof(friendlyName));
            password.ArgNotNull(nameof(password));

            // Download final certificate
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var certificate = await orderContext.Generate(new CsrInfo {
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
                .Build(friendlyName, password);
        }
    }
}
