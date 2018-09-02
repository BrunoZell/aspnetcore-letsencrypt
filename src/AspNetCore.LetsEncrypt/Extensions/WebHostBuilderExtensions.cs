using System;
using Microsoft.AspNetCore.Hosting;

namespace AspNetCore.LetsEncrypt.Extensions {
    public static class WebHostBuilderExtensions {
        public static IWebHostBuilder UseExternalConfiguration(this IWebHostBuilder webHostBuilder, Action<IWebHostBuilder> configureAction) {
            configureAction?.Invoke(webHostBuilder);
            return webHostBuilder;
        }
    }
}
