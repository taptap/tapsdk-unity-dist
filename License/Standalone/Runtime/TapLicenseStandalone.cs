using TapSDK.License;
using TapSDK.License.Internal;
using TapSDK.Core;
using TapSDK.Core.Standalone;
using System.Collections.Generic;
using System;
using UnityEngine.Animations;
using System.Linq;

#if PLATFORM_STANDALONE_WIN 
namespace TapSDK.License.Standalone {

    public class TapLicenseStandalone : ITapLicenseBridge
    {

        private List<ITapDlcCallback> currentDLCCallbacks;

        private List<ITapLicenseCallback> currentLicenseCallbacks;

        // 是否已在 dll 层注册回调
        private bool HasRegisterNativeDLCCallback = false;

        private bool HasRegisterNativeLicenseCallback = false;

        private string currentSessionId = null;

        public void Init()
        {
            TaplicenseTracker.Instance.TrackInit();
        }
        public void CheckLicense()
        {
            if (!CheckInit())
            {
                return;
            }
            currentSessionId = Guid.NewGuid().ToString();
            string method = "checkLicense";
            Dictionary<string, string> props = new Dictionary<string, string> { { "license_type", "tap_license" } };
            TaplicenseTracker.Instance.TrackStart(method, currentSessionId, props);
            bool isOwned = false;
            try
            {
                isOwned = TapClientStandalone.HasLicense();
            }
            catch (Exception e)
            {
                TaplicenseTracker.Instance.TrackFailure(method, currentSessionId, props, -1, e.Message ?? "");
                throw ;
            }

            if (isOwned)
            {
                TaplicenseTracker.Instance.TrackSuccess(method, currentSessionId, props);
            }
            else
            {
                TaplicenseTracker.Instance.TrackFailure(method, currentSessionId, props, 0, "NOT_PURCHASED");
            }

            if (currentLicenseCallbacks != null)
            {
                foreach (ITapLicenseCallback callback in currentLicenseCallbacks)
                {
                    if (isOwned)
                    {
                        callback?.OnLicenseSuccess();
                    }
                    else
                    {
                        callback?.OnLicenseFailed();
                    }
                }

            }
        }

        public void CheckLicenseForcibly()
        {
            CheckLicense();
        }


        public void QueryDLC(string[] skus)
        {
            if (!CheckInit())
            {
                return;
            }
            if (skus.Length == 0)
            {
                return;
            }
            currentSessionId = Guid.NewGuid().ToString();
            string method = "queryDLC";
            Dictionary<string, string> props = new Dictionary<string, string> { { "sku_ids", string.Join(",", skus) } };
            TaplicenseTracker.Instance.TrackStart(method, currentSessionId, props);
            Dictionary<string, object> dlcResult = new Dictionary<string, object>();
            try
            {
                foreach (string skuId in skus)
                {
                    bool isOwned = TapClientStandalone.QueryDLC(skuId);
                    dlcResult.Add(skuId, isOwned ? 1 : 0);
                }
            }
            catch (Exception e)
            {
                TaplicenseTracker.Instance.TrackFailure(method, currentSessionId, props, -1, e.Message ?? "");
                throw ;
            }
            TaplicenseTracker.Instance.TrackSuccess(method, currentSessionId, props);
            if (currentDLCCallbacks != null)
            {
                foreach (ITapDlcCallback callback in currentDLCCallbacks)
                {
                    callback?.OnQueryCallBack(TapLicenseQueryCode.QUERY_RESULT_OK, dlcResult);
                }
            }
        }

        public void RegisterDLCCallback(ITapDlcCallback callback)
        {
            if (currentDLCCallbacks == null)
            {
                currentDLCCallbacks = new List<ITapDlcCallback>();
            }
            currentDLCCallbacks.Add(callback);
            if (!HasRegisterNativeDLCCallback)
            {
                HasRegisterNativeDLCCallback = true;
                TapClientStandalone.RegisterDLCOwnedCallback(DLCCallbackDelegate);
            }
        }


        public void RegisterLicencesCallback(ITapLicenseCallback callback)
        {
            if (currentLicenseCallbacks == null)
            {
                currentLicenseCallbacks = new List<ITapLicenseCallback>();
            }
            currentLicenseCallbacks.Add(callback);
            if (!HasRegisterNativeLicenseCallback)
            {
                HasRegisterNativeLicenseCallback = true;
                TapClientStandalone.RegisterLicenseCallback(LicenseCallbackDelegate);
            }
        }


        public void SetTestEnvironment(bool isTest)
        {
            TapLogger.Warn($"{nameof(SetTestEnvironment)} NOT implemented.");
        }

        public void PurchaseDLC(string skuId)
        {
            if (!CheckInit())
            {
                return;
            }
            currentSessionId = Guid.NewGuid().ToString();
            string method = "purchaseDLC";
            Dictionary<string, string> props = new Dictionary<string, string> { { "sku_id", skuId } };
            TaplicenseTracker.Instance.TrackStart(method, currentSessionId, props);
            bool isSuccess = false;
            try
            {
                isSuccess = TapClientStandalone.ShowStore(skuId);
            }
            catch (Exception e)
            {
                TaplicenseTracker.Instance.TrackFailure(method, currentSessionId, props, -1, e.Message ?? "");
                throw;
            }
            if (isSuccess)
            {
                TaplicenseTracker.Instance.TrackSuccess(method, currentSessionId, props);
            }
            else
            {
                TaplicenseTracker.Instance.TrackFailure(method, currentSessionId, props, -1, "ShowStoreFailed");
            }
        }

        private void DLCCallbackDelegate(string skuId, bool isOwned)
        {
            if (currentDLCCallbacks != null)
            {
                foreach (ITapDlcCallback callback in currentDLCCallbacks)
                {
                    callback?.OnOrderCallBack(skuId, isOwned ? TapLicensePurchasedCode.DLC_PURCHASED : TapLicensePurchasedCode.DLC_NOT_PURCHASED);
                }
            }
        }


        private void LicenseCallbackDelegate(bool isOwned)
        {
            if (currentLicenseCallbacks != null)
            {
                foreach (ITapLicenseCallback callback in currentLicenseCallbacks)
                {
                    if (isOwned)
                    {
                        callback?.OnLicenseSuccess();
                    }
                    else
                    {
                        callback?.OnLicenseFailed();
                    }
                }

            }
        }

        private bool CheckInit()
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return false;
            }
            return true;
        }

    }
}
#endif