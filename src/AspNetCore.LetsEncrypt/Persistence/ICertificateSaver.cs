using System.Security.Cryptography.X509Certificates;

namespace AspNetCore.LetsEncrypt.Persistence {
    public interface ICertificateSaver {
        void Save(X509Certificate2 certificate, string commonName);
    }
}
