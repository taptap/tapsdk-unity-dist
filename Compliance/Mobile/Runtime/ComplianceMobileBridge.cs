using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TapSDK.Compliance.Model;
using System.Threading.Tasks;
using TapSDK.Core.Internal.Log;
using TapSDK.Core;
using UnityEngine;

namespace TapSDK.Compliance.Mobile.Runtime
{
    public static class ComplianceMobileBridge
    {
        private const string ANTI_ADDICTION_SERVICE = "BridgeComplianceService";
        private const string ANTI_ADDICTION_SERVICE_CLZ = "com.taptap.sdk.compliance.internal.enginebridge.BridgeComplianceService";
        private const string ANTI_ADDICTION_SERVICE_IMPL = "com.taptap.sdk.compliance.internal.enginebridge.BridgeComplianceServiceImpl";

        private static bool hasRegisterMobileCallback = false;
        private static List<Action<int, string>> callbackList = new List<Action<int, string>>();
        static ComplianceMobileBridge()
        {
            EngineBridge.GetInstance()
                .Register(ANTI_ADDICTION_SERVICE_CLZ, ANTI_ADDICTION_SERVICE_IMPL);
            TapLog.Log("ComplianceMobileBridge register.");
        }


        public static void Startup(string userIdentifier)
        {

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(ANTI_ADDICTION_SERVICE)
                .Method("startup")
                .Args("userId", userIdentifier)
                .CommandBuilder());

        }

        public static void RegisterComplianceCallback(Action<int, string> callback)
        {
            if (!callbackList.Contains(callback))
            {
                callbackList.Add(callback);
            }
            if (!hasRegisterMobileCallback)
            {
                var command = new Command.Builder()
                    .Service(ANTI_ADDICTION_SERVICE)
                    .Method("registerComplianceCallback")
                    .Callback(true)
                    .OnceTime(false)
                    .CommandBuilder();

                EngineBridge.GetInstance().CallHandler(command, result =>
                {
                    try
                    {
                        TapLog.Log($"[Unity:Compliance] callback result: {result.ToJSON().ToString()}");
                        if (result.code != Result.RESULT_SUCCESS)
                        {
                            callbackList.ForEach((item) =>
                            {
                                item?.Invoke(-1, "[Unity:Compliance] callback fail");
                            });
                            return;
                        }

                        if (string.IsNullOrEmpty(result.content))
                        {
                            callbackList.ForEach((item) =>
                            {
                                item?.Invoke(-1, "[Unity:Compliance] callback content is empty");
                            });
                            return;
                        }


                        ComplianceCallbackData callbackData = new ComplianceCallbackData();
#if UNITY_IOS
                    var callbackOriginData = JsonConvert.DeserializeObject<ComplianceCallbackOriginData>(result.content);
                    callbackData.code = callbackOriginData.code;

                    // 修复：检查extras是否为null或空，防止反序列化失败
                    if (!string.IsNullOrEmpty(callbackOriginData.extras))
                    {
                        callbackData.extras = JsonConvert.DeserializeObject<MsgExtraParams>(callbackOriginData.extras);
                        // 仅在文本字段上清理富文本，避免在整段 JSON 上跑正则时残缺标签跨界吞掉字段
                        if (callbackData.extras != null)
                        {
                            callbackData.extras.title = RemoveFontColor(callbackData.extras.title);
                            callbackData.extras.description = RemoveFontColor(callbackData.extras.description);
                        }
                    }
                    else
                    {
                        TapLog.Log("[Unity:Compliance] callback extras is null or empty, using default MsgExtraParams");
                        callbackData.extras = new MsgExtraParams();
                    }
#else
                        callbackData = JsonConvert.DeserializeObject<ComplianceCallbackData>(result.content);
#endif
                        if (StartUpResult.Contains(callbackData.code))
                        {
                            callbackList.ForEach((item) =>
                            {
                                item?.Invoke(callbackData.code, null);
                            });
                        }
                        else
                        {
                            TapLog.Log("[Unity:Compliance] result.extras title:" + callbackData.extras.title);
                            TapLog.Log("[Unity:Compliance] result.extras description:" + callbackData.extras.description);
                            callbackList.ForEach((item) =>
                            {
                                item?.Invoke(-1, callbackData.extras.description);
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        TapLog.Error("[Unity:Compliance] callback error:" + e.Message + "\n" + e.StackTrace);
                    }

                });
                hasRegisterMobileCallback = true;
            }
        }

        private static string RemoveFontColor(string originStr)
        {
            if (string.IsNullOrEmpty(originStr)) return originStr;
            // 去掉 <font ...> / </font> 与 <span ...> / </span> 标签（含带空格/不带空格的属性）
            // 用正则整体替换，避免手写循环在相邻标签(如 </font><br>)时 Remove 长度为 0 导致的死循环
            originStr = Regex.Replace(originStr, @"</?font[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            originStr = Regex.Replace(originStr, @"</?span[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            return originStr;
        }

        public static void SetTestEnvironment(bool isTest)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(ANTI_ADDICTION_SERVICE)
                .Method("setTestEnvironment")
                .Args("testEnv", isTest)
                .CommandBuilder());
        }



        public async static Task<string> GetCurrentAccessToken()
        {
            var command = new Command.Builder()
                .Service(ANTI_ADDICTION_SERVICE)
                .Method("getCurrentAccessToken")
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder();
            var result = await EngineBridge.GetInstance().Emit(command);
            if (!EngineBridge.CheckResult(result))
            {
                throw new TapException((int)TapErrorCode.ERROR_CODE_BRIDGE_EXECUTE, "TapCompliance CurrentToken Failed!");
            }
            var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
            var token = SafeDictionary.GetValue<string>(dic, "token") as string;
            return token;
        }



        public async static Task<int> GetCurrentUserRemainTime()
        {
            var command = new Command.Builder()
                 .Service(ANTI_ADDICTION_SERVICE)
                 .Method("getUserRemainTime")
                 .Callback(true)
                 .OnceTime(true)
                 .CommandBuilder();
            var result = await EngineBridge.GetInstance().Emit(command);
            if (!EngineBridge.CheckResult(result))
            {
                throw new TapException((int)TapErrorCode.ERROR_CODE_BRIDGE_EXECUTE, "TapCompliance CurrentUserRemainTime Failed!");
            }
            var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
            var remainTime = SafeDictionary.GetValue<int>(dic, "remainTime") as int?;
            return remainTime ?? 0;
        }

        public async static Task<int> GetUserAgeRange()
        {
            var command = new Command.Builder()
                 .Service(ANTI_ADDICTION_SERVICE)
                 .Method("getUserAgeRange")
                 .Callback(true)
                 .OnceTime(true)
                 .CommandBuilder();
            var result = await EngineBridge.GetInstance().Emit(command);
            if (!EngineBridge.CheckResult(result))
            {
                throw new TapException((int)TapErrorCode.ERROR_CODE_BRIDGE_EXECUTE, "TapCompliance CurrentUserAgeLimit Failed!");
            }
            var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
            var ageRange = SafeDictionary.GetValue<int>(dic, "ageRange") as int?;
            return ageRange ?? -1;
        }

        public static void Exit()
        {
            var command = new Command.Builder()
                .Service(ANTI_ADDICTION_SERVICE)
                .Method("exit")
                .Callback(false)
                .OnceTime(false)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

        public static void SubmitPayResult(long amount
            , Action handleSubmitPayResult
            , Action<string> handleSubmitPayResultException)
        {
            var command = new Command.Builder()
                .Service(ANTI_ADDICTION_SERVICE)
                .Method("submitPayment")
                .Args("submitAmount", amount)
                .Callback(true)
                .OnceTime(false)
                .CommandBuilder();

            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                TapLog.Log($"[Unity:Compliance] submitPayResult result: {result.ToJSON().ToString()}");
                if (result.code != Result.RESULT_SUCCESS)
                {
                    handleSubmitPayResultException?.Invoke("[Unity:Compliance] submitPayResult fail");
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    handleSubmitPayResultException?.Invoke("[Unity:Compliance] submitPayResult content is empty");
                    return;
                }

                TapLog.Log("[Unity:Compliance] submitPayResult content: " + result.content);
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                bool success = SafeDictionary.GetValue<int>(dic, "success") == 0;
                string errorMsg = SafeDictionary.GetValue<string>(dic, "description");
                if (success)
                {
                    handleSubmitPayResult?.Invoke();
                }
                else
                {
                    handleSubmitPayResultException?.Invoke(errorMsg);
                }
            });
        }

        public static void CheckPaymentLimit(long amount
            , Action<CheckPayResult> handleCheckPayLimit
            , Action<string> handleCheckPayLimitException)
        {
            var command = new Command.Builder()
                .Service(ANTI_ADDICTION_SERVICE)
                .Method("checkPaymentLimit")
                .Args("amount", amount)
                .Callback(true)
                .OnceTime(false)
                .CommandBuilder();

            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                TapLog.Log($"[Unity:Compliance] checkPayLimit result: {result.ToJSON().ToString()}");
                if (result.code != Result.RESULT_SUCCESS)
                {
                    handleCheckPayLimitException?.Invoke("[Unity:Compliance] checkPayLimit fail");
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    handleCheckPayLimitException?.Invoke("[Unity:Compliance] checkPayLimit content is empty");
                    return;
                }

                TapLog.Log("[Unity:Compliance] checkPayLimit content: " + result.content);

                var checkPayResultParams = JsonConvert.DeserializeObject<CheckPayResultParams>(result.content);
                if (checkPayResultParams.Success)
                {
                    handleCheckPayLimit?.Invoke(new CheckPayResult()
                    {
                        status = checkPayResultParams.status,
                        title = checkPayResultParams.title,
                        description = checkPayResultParams.description,
                    });
                }
                else
                {
                    handleCheckPayLimitException?.Invoke(checkPayResultParams.description);
                }
            });
        }

    }
}