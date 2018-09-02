using AspNetCore.LetsEncrypt.Internal.Abstractions;
using AspNetCore.LetsEncrypt.Internal.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCore.LetsEncrypt.Internal {
    internal class HostStartup {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAcmeChallengeListener();
            services.AddSingleton<IHttpChallengeResponseStore, InMemoryHttpChallengeResponseStore>();
            services.AddHostedService<AcmeChallengeRequester>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAcmeChallengeListener();
            app.Run(async context => {
                await context.Response.WriteAsync("Please fulfill the ACME challenge by requesting /.well-known/acme-challenge/{token}");
            });
        }
    }
}
