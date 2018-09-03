using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AspNetCore.LetsEncrypt.Internal.Extensions {
    internal static class ChallengeContextExtensions {
        public static async Task WaitForCompletion(this IChallengeContext challengeContext, TimeSpan pollInterval, ILogger logger = null)
        {
            // Get the challenges ressource to check if it's valid
            var challenge = await challengeContext.Resource();
            while (!challenge.HasFinished()) {
                // If nor finished processing, poll every second
                challenge = await challengeContext.Resource();
                await Task.Delay(pollInterval);
            }

            logger?.LogDebug("Http challenge finished with status {status}", challenge.Status?.ToString() ?? "[null]");
            if (challenge.Status == ChallengeStatus.Invalid) {
                // Throw if invalid
                new AcmeException(challenge.Error?.Detail ?? "ACME http challenge not successful.");
            }
        }

        private static bool HasFinished(this Challenge challenge) =>
            challenge.Status == ChallengeStatus.Valid || challenge.Status == ChallengeStatus.Invalid;
    }
}
