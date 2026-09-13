using System;
using System.Text;
using System.Threading.Tasks;
using TinyNakama.Helpers;
using UnityEngine.Networking;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TinyNakama
{
    public sealed class NakamaClient
    {
        public IAPHelper IAPHelper { get; private set; }
        public AccountHelper AccountHelper { get; private set; }
        public LeaderboardHelper LeaderboardHelper { get; private set; }

        private readonly NakamaSettings settings;
        public string sessionToken;
        public NakamaClient(NakamaSettings settings)
        {
            this.settings = settings;
            this.settings.Validate();
            IAPHelper = new IAPHelper(this);
            AccountHelper = new AccountHelper(this);
            LeaderboardHelper = new LeaderboardHelper(this);
        }

        public async Task<string> RpcAsync(string rpcId, string json = null, bool validateSyncResult = true)
        {
            var path = $"/v2/rpc/{Uri.EscapeDataString(rpcId)}?unwrap";
            var response = await SendAsync(UnityWebRequest.kHttpVerbPOST, path, json, true);
            if (validateSyncResult) ValidateSyncResult(rpcId, response);
            return response;
        }

        public async Task<T> RpcAsync<T>(string rpcId, string json = null, bool validateSyncResult = true)
        {
            return Deserialize<T>(await RpcAsync(rpcId, json, validateSyncResult), rpcId);
        }

        public async Task<string> SendAsync(string method, string path, string json = null, bool requiresSession = true)
        {
            if (requiresSession && string.IsNullOrWhiteSpace(sessionToken))
            {
                throw new InvalidOperationException("A Nakama session token is required.");
            }

            if (!path.StartsWith("/")) path = "/" + path;

            var url = settings.BaseUrl + path;
            using (var request = new UnityWebRequest(url, method))
            {
                if (method == UnityWebRequest.kHttpVerbPOST && !string.IsNullOrWhiteSpace(json))
                {
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                }
                request.timeout = settings.TimeoutSeconds;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", requiresSession ? $"Bearer {sessionToken}" : $"Basic {ToBase64(settings.ServerKey)}");

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();
                var body = request.downloadHandler.text;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("NAKAMA request failed " + $"url {url} {json} ({request.responseCode}): {request.error}\n body: {body}");
                    throw new NakamaException($"Nakama request failed " + $"({request.responseCode}): {request.error}", request.responseCode, body, request.result == UnityWebRequest.Result.ConnectionError);
                }
                Debug.Log("NAKAMA request success " + $"url {url} {json} body: {body}");
                return body;
            }
        }

        public static T Deserialize<T>(string response, string operation, bool snakeMoed = false)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver()
                    {
                        NamingStrategy = snakeMoed ? new SnakeCaseNamingStrategy() : null
                    }
                };
                return JsonConvert.DeserializeObject<T>(response, settings);
            }
            catch (Exception e)
            {
                throw new NakamaException($"Failed parsing Nakama response for " + $"'{operation}': {e.Message}", 200, response, false);
            }
        }

        private static void ValidateSyncResult(string rpcId, string response)
        {
            NakamaSyncResult result;
            try
            {
                result = JsonUtility.FromJson<NakamaSyncResult>(response);
            }
            catch (Exception e)
            {
                throw new NakamaException($"Failed parsing RPC '{rpcId}' result: {e.Message}", 200, response, false);
            }

            if (result == null) return;
            if (result.status != (int)NakamaStatus.Success)
            {
                throw new NakamaException($"RPC '{rpcId}' failed: {result.message}", 200, response, false, (NakamaStatus)result.status);
            }
        }

        private static string ToBase64(string serverKey) => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{serverKey}:"));

    }
}