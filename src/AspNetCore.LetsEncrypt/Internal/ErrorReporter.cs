using AspNetCore.LetsEncrypt.Exceptions;
using System;

namespace AspNetCore.LetsEncrypt.Internal {
    internal class ErrorReporter {
        private Exception _reportedException;

        public void ReportException(Exception exception) => _reportedException = exception;

        public void ThrowOnError()
        {
            if (_reportedException != null) {
                throw new LetsEncryptException("An error occured while ensuring an SSL-certificate exists.", _reportedException);
            }
        }
    }
}
