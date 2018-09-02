using System;

namespace AspNetCore.LetsEncrypt.Internal.Extensions {
    internal static class Guards {
        public static T ArgNotNull<T>(this T argument, string argumentName = null) where T : class =>
            argument ?? throw new ArgumentNullException(argumentName);

        public static T OptionNotNull<T>(this T option, string optionName) where T : class =>
            option ?? throw new LetsEncryptException($"{optionName} is a required option.");

        public static string OptionNotBlank(this string option, string optionName) =>
            String.IsNullOrWhiteSpace(option) ?
                throw new LetsEncryptException($"{optionName} is a required option.") :
                option;
    }
}
