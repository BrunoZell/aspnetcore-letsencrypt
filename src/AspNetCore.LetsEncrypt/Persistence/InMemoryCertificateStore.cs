using AspNetCore.LetsEncrypt.Internal.Extensions;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace AspNetCore.LetsEncrypt.Persistence {
    public partial class InMemoryCertificateStore {
        public ICertificateLoader Loader => new CertificateLoader(this);
        public ICertificateSaver Saver => new CertificateSaver(this);

        private ConcurrentDictionary<string, X509Certificate2> _certificates = new ConcurrentDictionary<string, X509Certificate2>();

        private void Save(X509Certificate2 certificate, string commonName) =>
            _certificates.AddOrUpdate(commonName, certificate, (_, __) => certificate);

        private bool TryLoad(string commonName, out X509Certificate2 certificate) =>
            _certificates.TryGetValue(commonName, out certificate);

        private class CertificateSaver : ICertificateSaver {
            private readonly InMemoryCertificateStore _store;

            public CertificateSaver(InMemoryCertificateStore store) =>
                _store = store.ArgNotNull(nameof(store));

            public void Save(X509Certificate2 certificate, string commonName) =>
                _store.Save(certificate, commonName);
        }

        private class CertificateLoader : ICertificateLoader {
            private readonly InMemoryCertificateStore _store;

            public CertificateLoader(InMemoryCertificateStore store) =>
                _store = store.ArgNotNull(nameof(store));

            public bool TryLoad(string commonName, out X509Certificate2 certificate) =>
                _store.TryLoad(commonName, out certificate);
        }
    }
}
