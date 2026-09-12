using System;
using UnityEngine;

namespace TinyNakama
{
    [Serializable]
    public sealed class NakamaSettings
    {
        [SerializeField] private int timeoutSeconds = 30;
        [SerializeField] private string url = "https://";
        [SerializeField] private string serverKey = "default-server-key";
        public string BaseUrl => url.TrimEnd('/');
        public string ServerKey => serverKey;
        public int TimeoutSeconds => timeoutSeconds;

        internal void Validate()
        {
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
             || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("Nakama BaseUrl must be an absolute HTTP or HTTPS URL.");
            }

            if (string.IsNullOrWhiteSpace(ServerKey))
            {
                throw new InvalidOperationException("Nakama server key is required.");
            }

            if (timeoutSeconds <= 0)
            {
                throw new InvalidOperationException("Nakama timeout must be greater than zero.");
            }
        }
    }
}