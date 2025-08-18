using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;
using TapSDK.Core.Internal;
using TapSDK.Core;

namespace TapSDK.Core.Mobile {
    public class IOSNativeWrapper
    {

#if UNITY_IOS
    // 导入 C 函数
        
        // 定义一个委托类型，匹配 Objective-C 中的 block 参数
        public delegate string DynamicPropertiesCalculatorDelegate();

        // 注意：这个方法的封装比较特殊，因为它需要一个返回 NSDictionary 的回调。
        [DllImport("__Internal")]
        private static extern void _TapTapEventRegisterDynamicProperties(DynamicPropertiesCalculatorDelegate callback);

        [DllImport("__Internal")]
        private static extern void _TapTapSDKCoreSwitchToRND();

        public static void SetRND(){
            _TapTapSDKCoreSwitchToRND();
        }
        // 定义一个 Func<string> 委托，用于从 Unity 使用者那里获取动态属性
        private static Func<string> dynamicPropertiesCallback;
        public static void RegisterDynamicProperties(Func<string> callback)
        {
            dynamicPropertiesCallback = callback;
            _TapTapEventRegisterDynamicProperties(DynamicPropertiesCalculator);
        }

        // Unity 端的回调方法，返回一个 JSON 字符串
        [AOT.MonoPInvokeCallback(typeof(DynamicPropertiesCalculatorDelegate))]
        private static string DynamicPropertiesCalculator()
        {
            if (dynamicPropertiesCallback != null)
            {
                string properties = dynamicPropertiesCallback();
                return properties;
            }
            return null;
        }

#endif
    }
}
