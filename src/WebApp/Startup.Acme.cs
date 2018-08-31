using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp {
    public class StartupAcme {
        public void ConfigureServices(IServiceCollection services) { }

        public void Configure(IApplicationBuilder app, IApplicationLifetime lifetime) {
            app.Run(async (context) => {
                await context.Response.WriteAsync("Please fulfill the ACME challenge!");
                lifetime.StopApplication();
            });
        }
    }
}
