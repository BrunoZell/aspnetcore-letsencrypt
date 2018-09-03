using AspNetCore.LetsEncrypt.Internal.Extensions;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace AspNetCore.LetsEncrypt.Persistence {
    public class FileCertificateLoader : ICertificateLoader {
        private readonly string _fileName;
        private readonly string _password;

        public FileCertificateLoader(string fileName, string password)
        {
            _fileName = fileName.ArgNotNull(nameof(fileName));
            _password = password.ArgNotNull(nameof(password));
        }

        public bool TryLoad(string commonName, out X509Certificate2 certificate)
        {
            if (File.Exists(_fileName)) {
                certificate = new X509Certificate2(_fileName, _password);
                return true;
            }

            certificate = null;
            return false;
        }
    }
}
