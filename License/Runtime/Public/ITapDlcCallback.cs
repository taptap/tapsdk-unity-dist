using System.Collections.Generic;

namespace TapSDK.License {
    public interface ITapDlcCallback {
        void OnQueryCallBack(TapLicenseQueryCode code, Dictionary<string, object> queryList);
        void OnOrderCallBack(string sku, TapLicensePurchasedCode status);
    }
}
