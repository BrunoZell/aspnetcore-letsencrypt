using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebApp.Internal {
    internal class AcmeChallengeMiddleware {
        private readonly RequestDelegate next;

        public AcmeChallengeMiddleware(RequestDelegate next) {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, IHttpChallengeResponseStore responseStore) {
            string token = context.Request.Path.ToString().Replace(".well-known/acme-challenge", "").Trim('/');
            if (!responseStore.TryGetResponse(token, out string response)) {
                await context.Response.WriteAsync("ACME challenge token unknown");
                return;
            }

            context.Response.ContentLength = response.Length;
            context.Response.ContentType = "application/octet-stream";
            await context.Response.WriteAsync(response, context.RequestAborted);
        }
    }
}
