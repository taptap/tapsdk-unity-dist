using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;
using TapSDK.Core;
using TapSDK.Core.Internal;
using System.Collections.Generic;

namespace TapSDK.Core.Mobile{
    internal class AndroidNativeWrapper
    {
        private static AndroidJavaClass tapTapEventClass;
        
        public static void RegisterDynamicProperties(Func<string> callback)
        {
            tapTapEventClass = new AndroidJavaClass("com.taptap.sdk.core.TapTapEvent");
            AndroidJavaProxy dynamicPropertiesProxy = new TapEventDynamicPropertiesProxy(callback);
            tapTapEventClass.CallStatic("registerDynamicProperties", dynamicPropertiesProxy);
        }

        private class TapEventDynamicPropertiesProxy : AndroidJavaProxy
        {
            private Func<string> callback;

            public TapEventDynamicPropertiesProxy(Func<string> callback)
                : base("com.taptap.sdk.core.TapTapEvent$TapEventDynamicProperties")
            {
                this.callback = callback;
            }

            public AndroidJavaObject getDynamicProperties()
            {
                try
                {
                    string json = callback();
                    if (!string.IsNullOrEmpty(json))
                    {
                        return new AndroidJavaObject("org.json.JSONObject", json);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to get dynamic properties: " + e.Message);
                }
                return null;
            }
        }

    }
}