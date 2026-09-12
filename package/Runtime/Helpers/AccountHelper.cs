using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using UnityEngine;

namespace TinyNakama.Helpers
{

    [Serializable]
    public sealed class CustomAuthenticationRequest
    {
        public string id;
        public AuthenticationVars vars;
    }

    [Serializable]
    public sealed class AuthenticationVars
    {
        public string device, displayName, avatarUrl, location, timezone, store, first_version, latest_version, langTag;
    }

    [Serializable]
    public sealed class NakamaAuthenticationResult
    {
        public string token;
        public string refresh_token;
        public bool created;
    }

    public class AccountVariant
    {
        public string name = "", value = "A";
    }

    public class FacebookLoginResult
    {
        public bool linked;
        public bool alreadyInUse;
        public bool networkError;
        public string failure;
        public string customId;
        public string displayName;
        public string avatarUrl;
        public long createTime;
        public long updateTime;
        public int score;
    }


    public class AccountHelper : NakamaHelper
    {
        public AccountHelper(NakamaClient client) : base(client) { }

        public async Task<NakamaAuthenticationResult> AuthenticateCustom(string id,
        string device,
         string displayName,
         string avatarUrl,
         string location,
         string timezone,
         string store,
        string firstVersion,
         string latestVersion,
         string langTag)
        {
            var payload = new CustomAuthenticationRequest
            {
                id = id,
                vars = new AuthenticationVars
                {
                    device = device,
                    displayName = displayName,
                    avatarUrl = avatarUrl,
                    location = location,
                    timezone = timezone,
                    store = store,
                    first_version = firstVersion,
                    latest_version = latestVersion,
                    langTag = langTag,
                }
            };

            var json = JsonUtility.ToJson(payload);
            var response = await client.SendAsync(UnityWebRequest.kHttpVerbPOST, "/v2/account/authenticate/custom?create=true", json, false);
            var result = NakamaClient.Deserialize<NakamaAuthenticationResult>(response, "custom authentication");
            if (result == null || string.IsNullOrWhiteSpace(result.token))
            {
                throw new NakamaException("Custom authentication returned no session token.", 200, response, false);
            }

            client.sessionToken = result.token;
            return result;
        }

        public async Task<AccountVariant> GetVariant()
        {
            try
            {
                var response = await client.RpcAsync("account_get", "{}");
                var metadata = JObject.Parse(response).SelectToken("data.user.metadata");
                var name = metadata?.Value<string>("variant_name");
                if (string.IsNullOrEmpty(name)) return new AccountVariant();

                var value = metadata.Value<string>("variant_value");
                return new AccountVariant { name = name, value = string.IsNullOrEmpty(value) ? "A" : value };
            }
            catch (Exception e)
            {
                Debug.LogError($"variant parse error: {e.Message}");
                return new AccountVariant();
            }
        }


        public async Task<bool> UnlinkAccount()
        {
            try
            {
                var response = await client.RpcAsync("account_unlink");
                Debug.Log($"response :{response}");
                try
                {
                    return JObject.Parse(response).Value<int?>("status") == (int)NakamaStatus.Success;
                }
                catch (Exception e)
                {
                    Debug.LogError($"account unlink parse error: {e.Message}");
                    return false;
                }
            }
            catch (NakamaException e)
            {
                Debug.LogError($"account unlik ({(e.IsNetworkError ? "network" : "server")}): {e.Message}"
                    + $"\nResponseBody: {e.ResponseBody}");
                return false;
            }
        }

        public async Task<FacebookLoginResult> LinkFacebook(string facebookId, string facebookToken)
        {
            var payloadJson = JsonConvert.SerializeObject(new Dictionary<string, string> { ["fb_id"] = facebookId, ["fb_client_token"] = facebookToken });
            try
            {
                var response = await client.RpcAsync("account_link_facebook", payloadJson);
                return Parse(response);
            }
            catch (NakamaException e)
            {
                Debug.LogError($"facebook link failed ({(e.IsNetworkError ? "network" : "server")}): {e.Message} \nResponseBody: {e.ResponseBody}");
                return new FacebookLoginResult { failure = e.Message, networkError = e.IsNetworkError };
            }
        }

        private FacebookLoginResult Parse(string response)
        {
            try
            {
                var body = JObject.Parse(response);
                var status = (NakamaStatus)body.Value<int?>("status");
                var message = body.Value<string>("message");
                var result = new FacebookLoginResult();
                var data = body["data"];

                if (status == NakamaStatus.Success)
                {
                    var user = data?["user"];
                    result.linked = true;
                    result.customId = data?.Value<string>("customId");
                    result.displayName = user?.Value<string>("displayName");
                    result.avatarUrl = user?.Value<string>("avatarUrl");
                    result.createTime = user?.Value<long?>("createTime") ?? 0;
                    result.updateTime = user?.Value<long?>("updateTime") ?? 0;
                    result.score = data?["wallet"]?.Value<int?>("score") ?? 0;
                    return result;
                }

                if (status == NakamaStatus.AlreadyExists)
                {
                    result.alreadyInUse = true;
                    result.customId = data.Value<string>("custom_id");
                    result.displayName = data.Value<string>("display_name");
                    result.avatarUrl = data.Value<string>("avatar_url");
                    result.createTime = data.Value<long>("create_time");
                    result.updateTime = data.Value<long>("update_time");
                    result.score = data.Value<int>("score");
                }

                result.failure = string.IsNullOrEmpty(message) ? $"server status {status}" : message;
                return result;
            }
            catch (Exception e)
            {
                return new FacebookLoginResult { failure = $"parse error: {e.Message}" };
            }
        }
    }
}