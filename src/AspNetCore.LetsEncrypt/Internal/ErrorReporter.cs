using System;
using AspNetCore.LetsEncrypt.Exceptions;

namespace AspNetCore.LetsEncrypt.Internal {
    internal class ErrorReporter {
        private Exception reportedException;

        public void ReportException(Exception exception) => reportedException = exception;

        public void ThrowOnError() {
            if (reportedException != null) {
                throw new LetsEncryptException("An error occured while ensuring an SSL-certificate exists.", reportedException);
            }
        }
    }
}
