using System;

namespace AspNetCore.LetsEncrypt.Options {
    public class LetsEncryptOptions {
        public string Email { get; set; }
        public string AccountKey { get; set; }
        public string Hostname { get; set; }
        public string FriendlyName { get; set; }
        public TimeSpan RenewalBuffer { get; set; }
        public CsrInfo CsrInfo { get; set; }
        public Authority Authority { get; set; } = new Authority();
    }
}
