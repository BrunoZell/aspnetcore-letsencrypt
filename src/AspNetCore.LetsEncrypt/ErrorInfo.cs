namespace AspNetCore.LetsEncrypt {
    public class ErrorInfo {
        public bool Continue { get; set; }
        public LetsEncryptException Exception { get; set; }
    }
}
