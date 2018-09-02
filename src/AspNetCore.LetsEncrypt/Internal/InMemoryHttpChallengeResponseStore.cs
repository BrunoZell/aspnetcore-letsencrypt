﻿using AspNetCore.LetsEncrypt.Internal.Abstractions;
using System.Collections.Concurrent;

namespace AspNetCore.LetsEncrypt.Internal {
    internal class InMemoryHttpChallengeResponseStore : IHttpChallengeResponseStore {
        private ConcurrentDictionary<string, string> _values = new ConcurrentDictionary<string, string>();

        public void AddChallengeResponse(string token, string response)
            => _values.AddOrUpdate(token, response, (_, __) => response);

        public bool TryGetResponse(string token, out string value)
            => _values.TryGetValue(token, out value);
    }
}
