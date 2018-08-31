using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApp.Options;

namespace WebApp {
    public class StartupAcme {
        public IConfiguration Configuration { get; }

        public StartupAcme(IConfiguration configuration) {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services) {
            services.AddOptions();
            services.Configure<LetsEncryptOptions>(Configuration.GetSection(LetsEncryptOptions.SectionName));

            services.AddMemoryCache();
            services.AddSingleton<IHostedService, AcmeService>();
        }

        public void Configure(IApplicationBuilder app, IMemoryCache memoryCache) {
            app.Map("/.well-known/acme-challenge", mapped => {
                app.Run(async context => {
                    var token = context.Request.Path.ToString().Replace(".well-known/acme-challenge", "").Trim('/');
                    if (!memoryCache.TryGetValue("token", out string expectedToken) ||
                        !memoryCache.TryGetValue("response", out string responseKey)) {
                        await context.Response.WriteAsync("No ACME challenge requested yet");
                        return;
                    }

                    if (expectedToken?.Equals(token, StringComparison.InvariantCultureIgnoreCase) != true) {
                        await context.Response.WriteAsync("No valid token transmitted");
                        return;
                    }

                    context.Response.ContentLength = responseKey.Length;
                    context.Response.ContentType = "application/octet-stream";
                    await context.Response.WriteAsync(responseKey, context.RequestAborted);
                });
            });

            app.Run(async context => {
                await context.Response.WriteAsync("Please fulfill the ACME challenge by requesting /.well-known/acme-challenge/{token}");
            });
        }
    }
}
