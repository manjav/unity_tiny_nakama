using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace TinyNakama.Helpers
{
    public class IAPVerifyData
    {
        public bool valid;
        public bool seenBefore;
        public string productId;
        public float price;
        public decimal localizedPrice;
        public string currencyCode;
        public string transactionId;
        public string environment;
        public string failure;
        public bool networkError;
        public Dictionary<string, object> ToMetadata() => new()
        {
            ["valid"] = valid,
            ["seen_before"] = seenBefore,
            ["product_id"] = productId,
            ["price"] = price,
            ["localized_price"] = localizedPrice,
            ["currency_code"] = currencyCode,
            ["transaction_id"] = transactionId,
            ["environment"] = environment,
        };
    }



    public sealed class IAPHelper : NakamaHelper
    {
        public IAPHelper(NakamaClient client) : base(client) { }

        public async Task<IAPVerifyData> Verify(string receipt, string productId, string transactionId, Dictionary<string, int> payout, string currencyCode, decimal localPrice)
        {
            if (string.IsNullOrEmpty(receipt))
            {
                Debug.LogError("purchase verify no receipt");
                return new IAPVerifyData { failure = "no receipt" };
            }

            var payloadJson = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                ["receipt"] = receipt,
                ["payout"] = payout,
                ["currency_code"] = currencyCode,
                ["local_price"] = localPrice,
            });

            try
            {
                Debug.Log($"call purchaseverfy > {payloadJson}");
                var response = await client.RpcAsync("purchase_verify", payloadJson);
                Debug.Log($"purchase verification success {productId}");
                return Parse(response, productId, transactionId);
            }
            catch (NakamaException e)
            {
                Debug.LogError($"purchase verify failed '{productId}' ({(e.IsNetworkError ? "network" : "server")}): {e.Message}"
                    + $"\nResponseBody: {e.ResponseBody}");
                return new IAPVerifyData { failure = e.Message, networkError = e.IsNetworkError };
            }
        }

        private IAPVerifyData Parse(string response, string productId, string transactionId)
        {
            try
            {
                var body = JObject.Parse(response);
                var status = body.Value<int?>("status") ?? -1;
                if (status != 0)
                    return new IAPVerifyData { failure = $"server status {status}: {body.Value<string>("message")}" };

                if (body["data"] is not JArray purchases || purchases.Count == 0)
                    return new IAPVerifyData { failure = "no validated purchase in the response" };

                var entry = purchases.FirstOrDefault(p => p.Value<string>("transactionId") == transactionId)
                         ?? purchases.FirstOrDefault(p => p.Value<string>("productId") == productId);
                if (entry == null)
                    return new IAPVerifyData { failure = $"'{productId}' and '{transactionId}' are null" };

                if (entry.Value<long>("refundTime") != 0)
                    return new IAPVerifyData { failure = "this purchase was refunded" };

                return new IAPVerifyData
                {
                    valid = true,
                    seenBefore = entry.Value<bool>("seenBefore"),
                    productId = entry.Value<string>("productId"),
                    transactionId = entry.Value<string>("transactionId"),
                    environment = entry.Value<string>("environment"),
                };
            }
            catch (Exception e)
            {
                return new IAPVerifyData { failure = $"parse error: {e.Message}" };
            }
        }
    }
}


