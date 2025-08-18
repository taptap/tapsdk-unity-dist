using UnityEngine;
using UnityEngine.Purchasing.Extension;

namespace TapSDK.IAP
{
    public class TapPurchasingModule : AbstractPurchasingModule
    {

        private static TapPurchasingModule instance = null;


        public static TapPurchasingModule Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapPurchasingModule();
                }
                return instance;
            }
        }

        public override void Configure()
        {
            var taptapStore = Application.platform == RuntimePlatform.Android ? new TapTapStore() : null;
            RegisterStore("TapTap", taptapStore);
            BindConfiguration<ITapTapStoreConfiguration>(taptapStore);
        }

    }
}