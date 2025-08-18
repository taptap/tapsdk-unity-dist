using System.Collections.Generic;
using TapSDK.Core;
using TapSDK.IAP;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace TapSDK.IAP.Mobile
{
    public class SDKInitConfig
    {
        public static int ReginCode_CN = 0;
        public static int ReginCode_Global = 1;
        public int RegionCode { get; set; }// 0 = CN 1 = Global
        public bool EnableLog { get; set; }
        public bool IsRNDMode { get; set; }

        public SDKInitConfig(int regionCode, bool enableLog, bool isRNDMode)
        {
            RegionCode = regionCode;
            EnableLog = enableLog;
            IsRNDMode = isRNDMode;
        }
    }

    public class TapTapIAPMobile : ITapTapIAPBridge
    {
        public static string TAP_IAP_SERVICE = "BridgePaymentService";

        public static string TDS_IAP_SERVICE_CLZ = "com.taptap.payment.sdk.unity.BridgePaymentService";

        public static string TDS_IAP_SERVICE_IMPL = "com.taptap.payment.sdk.unity.BridgePaymentServiceImpl";

        public TapTapIAPMobile()
        {
            EngineBridge.GetInstance().Register(TDS_IAP_SERVICE_CLZ, TDS_IAP_SERVICE_IMPL);
        }

        public void Initialize(string clientId, string clientToken, int regionCode, bool enableLog, bool isRNDMode)
        {
            var config = new SDKInitConfig(regionCode, enableLog, isRNDMode);
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };


            // 将对象转换为 JSON 字符串
            string configStr = JsonConvert.SerializeObject(config, settings);
            var command = new Command.Builder()
                            .Service(TAP_IAP_SERVICE)
                            .Method("initSDK")
                            .Args("clientId", clientId)
                            .Args("clientToken", clientToken)
                            .Args("initConfig", configStr)
                            .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

        public void RetrieveProducts(string[] productIds, ITapTapIAPRetrieveProductCallback callback)
        {
            var command = new Command.Builder()
                .Service(TAP_IAP_SERVICE)
                .Method("retrieveProducts")
                .Args("products", productIds)
                .Callback(true)
                .OnceTime(false)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, (result) =>
            {
                if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                {
                    callback.OnQueryFailed(result.code, result.message + $"content = {result.content}");
                    return;
                }

                ApiResponse response = null;
                try
                {
                    response = JsonConvert.DeserializeObject<ApiResponse>(result.content);
                }
                catch (JsonException ex)
                {
                    callback.OnQueryFailed(Result.RESULT_ERROR, $"Failed to parse JSON: {ex.Message}");
                    return;
                }

                if (response != null && response.Code == 0)
                {
                    callback.OnQuerySuccess(response.Content.ToArray());
                }
                else
                {
                    callback.OnQueryFailed(result.code, response?.Message ?? "Unknown error");
                }
            });
        }

        public void PurchaseProduct(string productId, string payload, ITapTapIAPPurchaseProductCallback callback)
        {
            var command = new Command.Builder()
                            .Service(TAP_IAP_SERVICE)
                            .Method("purchase")
                            .Args("productId", productId)
                            .Args("payload", payload)
                            .Callback(true)
                            .OnceTime(false)
                            .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, (result) =>
            {
                if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                {
                    callback.OnPurchaseFailed(productId, result.code, result.message + $"content = {result.content}");
                    return;
                }

                PurchaseApiResponse response = null;
                try
                {
                    response = JsonConvert.DeserializeObject<PurchaseApiResponse>(result.content);
                }
                catch (JsonException ex)
                {
                    callback.OnPurchaseFailed(productId, Result.RESULT_ERROR, $"purchase Failed to parse JSON: {ex.Message}");
                    return;
                }

                if (response != null && response.Code == 0 && response.Content.ProductId == productId && !string.IsNullOrEmpty(response.Content.PurchaseToken) && !string.IsNullOrEmpty(response.Content.OrderId))
                {
                    callback.OnPurchaseSuccess(productId, response.Content.PurchaseToken, response.Content.OrderId);
                }
                else
                {
                    callback.OnPurchaseFailed(productId, response.Code, $"illegal purchase result{response}");
                }

            });
        }


        public void FinishTransaction(string transactionId)
        {
            var command = new Command.Builder()
                            .Service(TAP_IAP_SERVICE)
                            .Method("finishTransaction")
                            .Args("transactionId", transactionId)
                            .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

        public void RestorePurchase(ITapTapIAPUnFinishPurchaseCallback callback)
        {
            var command = new Command.Builder()
                .Service(TAP_IAP_SERVICE)
                .Method("restorePurchase")
                .Callback(true)
                .OnceTime(false);

            EngineBridge.GetInstance().CallHandler(command.CommandBuilder(), (result) =>
            {
                if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                {
                    callback.OnFetchFailed(result.code, result.message + $"content = {result.content}");
                    return;
                }

                RestorePurchaseApiResponse response = null;
                try
                {
                    response = JsonConvert.DeserializeObject<RestorePurchaseApiResponse>(result.content);
                }
                catch (JsonException ex)
                {
                    callback.OnFetchFailed(Result.RESULT_ERROR, $"purchase Failed to parse JSON: {ex.Message}");
                    return;
                }

                if (response.Code == 0)
                {
                    callback.OnFetchSuccess(response.Content.ToArray());
                }
                else
                {
                    callback.OnFetchFailed(response.Code, $"restore failed internal {response}");
                }
            });
        }

    }
}