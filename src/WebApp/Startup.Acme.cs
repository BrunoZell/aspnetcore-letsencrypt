using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApp.Extensions;
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

            services.AddAcmeChallenge();
            services.AddSingleton<IHttpChallengeResponseStore, InMemoryHttpChallengeResponseStore>();
            services.AddSingleton<IHostedService, AcmeService>();
        }

        public void Configure(IApplicationBuilder app) {
            app.UseAcmeChallenge();
            app.Run(async context => {
                await context.Response.WriteAsync("Please fulfill the ACME challenge by requesting /.well-known/acme-challenge/{token}");
            });
        }
    }
}
