using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using WebApp.Extensions;

namespace WebApp.Internal {
    public class AcmeStartupFilter : IStartupFilter {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app => {
                app.UseAcmeChallenge();
                app.Run(async context => {
                    await context.Response.WriteAsync("Please fulfill the ACME challenge by requesting /.well-known/acme-challenge/{token}");
                });

                next(app);
            };
    }
}
