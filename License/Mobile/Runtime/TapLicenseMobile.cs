using System.Collections.Generic;
using TapSDK.License.Internal;
using TapSDK.Core;

namespace TapSDK.License.Mobile
{
    public class TapLicenseMobile : ITapLicenseBridge
    {
        public static string TAP_LICENSE_SERVICE = "BridgeLicenseService";

        public static string TDS_LICENSE_SERVICE_CLZ = "com.taptap.sdk.license.internal.enginebridge.BridgeLicenseService";

        public static string TDS_LICENSE_SERVICE_IMPL = "com.taptap.sdk.license.internal.enginebridge.BridgeLicenseServiceImpl";

        public TapLicenseMobile()
        {
            EngineBridge.GetInstance().Register(TDS_LICENSE_SERVICE_CLZ, TDS_LICENSE_SERVICE_IMPL);
        }

        public void Init()
        {
            // Android 原生内部处理
        }

        public void RegisterLicencesCallback(ITapLicenseCallback callback)
        {
            var command = new Command.Builder()
                .Service(TAP_LICENSE_SERVICE)
                .Method("registerLicenseCallback")
                .Callback(true)
                .OnceTime(false)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, (result) =>
            {
                if (result.code != Result.RESULT_SUCCESS)
                {
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    return;
                }
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var success = SafeDictionary.GetValue<string>(dic, "login") as string;
                if (success.Equals("success"))
                {
                    callback.OnLicenseSuccess();
                }
            });
        }

        public void RegisterDLCCallback(ITapDlcCallback callback)
        {
            var command = new Command.Builder();
            command.Service(TAP_LICENSE_SERVICE);
            command.Method("registerDLCCallback")
                .Callback(true)
                .OnceTime(false);
           
            EngineBridge.GetInstance().CallHandler(command.CommandBuilder(), (result) =>
            {
                if (result.code != Result.RESULT_SUCCESS)
                {
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    return;
                }
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var dlc = SafeDictionary.GetValue<string>(dic, "orderDLC");
                if (!string.IsNullOrEmpty(dlc))
                {
                    var statusCode = SafeDictionary.GetValue<int>(dic, "orderStatus");
                    callback.OnOrderCallBack(dlc, HandlePurchasedCode(statusCode));
                    return;
                }
                var code = SafeDictionary.GetValue<int>(dic, "queryCode");
                var queryListJson = SafeDictionary.GetValue<string>(dic, "queryResult");
                var queryListDic = Json.Deserialize(queryListJson) as Dictionary<string, object>;
                callback.OnQueryCallBack(HandleQueryCode(code), queryListDic);
            });
        }

        private static TapLicenseQueryCode HandleQueryCode(int code)
        {
            switch (code)
            {
                case 0:
                    return TapLicenseQueryCode.QUERY_RESULT_OK;
                case 1:
                    return TapLicenseQueryCode.QUERY_RESULT_NOT_INSTALL_TAPTAP;
                case 2:
                    return TapLicenseQueryCode.QUERY_RESULT_ERR;
                default:
                    return TapLicenseQueryCode.ERROR_CODE_UNDEFINED;
            }
        }

        private static TapLicensePurchasedCode HandlePurchasedCode(int code)
        {
            switch (code)
            {
                case 0:
                    return TapLicensePurchasedCode.DLC_NOT_PURCHASED;
                case 1:
                    return TapLicensePurchasedCode.DLC_PURCHASED;
                case -1:
                    return TapLicensePurchasedCode.DLC_RETURN_ERROR;
                default:
                    return TapLicensePurchasedCode.ERROR_CODE_UNDEFINED;
            }
        }

        public void CheckLicense()
        {
            var command = new Command.Builder()
                .Service(TAP_LICENSE_SERVICE)
                .Method("checkLicense")
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

        public void CheckLicenseForcibly(){
             var command = new Command.Builder()
                .Service(TAP_LICENSE_SERVICE)
                .Method("checkLicenseForcibly")
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

        public void QueryDLC(string[] skus)
        {
            var command = new Command.Builder()
                .Service(TAP_LICENSE_SERVICE)
                .Method("queryDLC")
                .Args("skuList", skus)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

        public void PurchaseDLC(string sku)
        {
            var command = new Command.Builder()
                .Service(TAP_LICENSE_SERVICE)
                .Method("purchaseDLC")
                .Args("skuId", sku)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

        public void SetTestEnvironment(bool isTest){
            var command = new Command.Builder()
                .Service(TAP_LICENSE_SERVICE)
                .Method("setTestEnvironment")
                .Args("testEnv", isTest)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

    }
}