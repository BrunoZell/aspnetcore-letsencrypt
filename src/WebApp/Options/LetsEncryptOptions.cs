using System;

namespace WebApp.Options {
    public class LetsEncryptOptions {
        public string Email { get; set; }
        public string AccountKey { get; set; }
        public string Hostname { get; set; }
        public TimeSpan RenewalBuffer { get; set; }
        public Certificate Certificate { get; set; }
        public CsrInfo CsrInfo { get; set; }
    }
}
