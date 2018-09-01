using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApp.Internal;
using WebApp.Options;

namespace WebApp {
    internal class StartupAcme {
        public IConfiguration Configuration { get; }

        public StartupAcme(IConfiguration configuration) {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services) {
            services.AddOptions();
            services.Configure<LetsEncryptOptions>(Configuration.GetSection(LetsEncryptOptions.SectionName));

            services.AddSingleton<IHttpChallengeResponseStore, InMemoryHttpChallengeResponseStore>();
            services.AddSingleton<IHostedService, AcmeService>();
        }

        public void Configure(IApplicationBuilder app, IHttpChallengeResponseStore responseStore) {
            app.Map("/.well-known/acme-challenge", mapped => {
                app.Run(async context => {
                    string token = context.Request.Path.ToString().Replace(".well-known/acme-challenge", "").Trim('/');
                    if (!responseStore.TryGetResponse(token, out string response)) {
                        await context.Response.WriteAsync("ACME challenge token unknown");
                        return;
                    }

                    context.Response.ContentLength = response.Length;
                    context.Response.ContentType = "application/octet-stream";
                    await context.Response.WriteAsync(response, context.RequestAborted);
                });
            });

            app.Run(async context => {
                await context.Response.WriteAsync("Please fulfill the ACME challenge by requesting /.well-known/acme-challenge/{token}");
            });
        }
    }
}
