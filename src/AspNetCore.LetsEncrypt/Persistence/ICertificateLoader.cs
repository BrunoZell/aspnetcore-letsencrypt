using System.Security.Cryptography.X509Certificates;

namespace AspNetCore.LetsEncrypt.Persistence {
    public interface ICertificateLoader {
        bool TryLoad(string commonName, out X509Certificate2 certificate);
    }
}
