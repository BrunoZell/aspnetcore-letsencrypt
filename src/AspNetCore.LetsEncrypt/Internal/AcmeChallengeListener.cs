using AspNetCore.LetsEncrypt.Internal.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.LetsEncrypt.Internal {
    internal class AcmeChallengeListener {
        public AcmeChallengeListener(RequestDelegate next) { }

        public async Task InvokeAsync(HttpContext context, IHttpChallengeResponseStore responseStore, IEnumerable<ILogger> loggers)
        {
            var logger = loggers.FirstOrDefault();
            string token = context.GetRouteValue("acmeToken") as string;
            logger?.LogDebug("ACME http challenge request with token {token} received.", token);
            if (!responseStore.TryGetResponse(token, out string response)) {
                logger?.LogWarning("Transmitted ACME http challenge token invalid. Token received: {token}", token);
                await context.Response.WriteAsync("ACME challenge token invalid");
                return;
            }

            logger?.LogInformation("Transmitted ACME http challenge token valid.");
            context.Response.ContentLength = response.Length;
            context.Response.ContentType = "application/octet-stream";
            await context.Response.WriteAsync(response);
        }
    }
}
