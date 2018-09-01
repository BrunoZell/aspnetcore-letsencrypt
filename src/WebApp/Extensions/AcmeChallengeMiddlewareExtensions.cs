using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Internal;

namespace WebApp.Extensions {
    public static class AcmeChallengeMiddlewareExtensions {
        public static IApplicationBuilder UseAcmeChallenge(this IApplicationBuilder app) {
            var router = new RouteBuilder(app)
                .MapMiddlewareGet("/.well-known/acme-challenge/{*acmeToken}", a => a.UseMiddleware<AcmeChallengeMiddleware>())
                .Build();

            app.UseRouter(router);
            return app;
        }

        public static IServiceCollection AddAcmeChallenge(this IServiceCollection services) {
            services.AddRouting();
            return services;
        }
    }
}
