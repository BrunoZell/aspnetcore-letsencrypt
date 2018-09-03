using AspNetCore.LetsEncrypt.Internal.Extensions;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace AspNetCore.LetsEncrypt.Persistence {
    public class FileCertificateSaver : ICertificateSaver {
        private readonly string _fileName;
        private readonly X509ContentType _format;
        private readonly string _password;

        public FileCertificateSaver(string fileName, X509ContentType format, string password)
        {
            _fileName = fileName.ArgNotNull(nameof(fileName));
            _format = format;
            _password = password.ArgNotNull(nameof(password));
        }

        public void Save(X509Certificate2 certificate, string commonName)
        {
            byte[] exportedData = certificate.Export(_format, _password);
            File.WriteAllBytes(_fileName, exportedData);
        }
    }
}
