using System;

namespace AspNetCore.LetsEncrypt {
    public class LetsEncryptException : Exception {
        public LetsEncryptException() { }

        public LetsEncryptException(string message)
            : base(message) { }

        public LetsEncryptException(string message, Exception inner)
            : base(message, inner) { }
    }
}
