using Certes.Acme;

namespace WebApp.Options {
    public class Authority {
        public string Name { get; set; } = "CN=Let's Encrypt Authority X3";
        public string DirectoryUri { get; set; } = WellKnownServers.LetsEncryptV2.ToString();
    }
}
