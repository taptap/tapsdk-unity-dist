using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using TapSDK.Core.Internal;
using System.Collections.Generic;
using TapSDK.Core.Internal.Log;
using UnityEngine;

namespace TapSDK.Core.Mobile
{
    public class TapEventMobile : ITapEventPlatform
    {
        private EngineBridge Bridge = EngineBridge.GetInstance();

        public TapEventMobile()
        {
            TapLog.Log("TapEventMobile constructor");
            EngineBridgeInitializer.Initialize();
        }

        public void Init(TapTapEventOptions eventOptions)
        {
            
        }

        public void SetUserID(string userID)
        {
            TapLog.Log("TapEventMobile SetUserID = " + userID);
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("setUserID")
                .Args("userID", userID)
                .CommandBuilder());
        }

        public void SetUserID(string userID, string properties)
        {
            TapLog.Log("TapEventMobile SetUserID" + userID + properties);
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("setUserID")
                .Args("userID", userID)
                .Args("properties", properties)
                .CommandBuilder());
        }

        public void ClearUser()
        {
            TapLog.Log("TapEventMobile ClearUser");
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("clearUser")
                .CommandBuilder());
        }

        public string GetDeviceId()
        {
            string deviceId = Bridge.CallWithReturnValue(EngineBridgeInitializer.GetBridgeServer()
                .Method("getDeviceId")
                .CommandBuilder());
            TapLog.Log("TapEventMobile GetDeviceId = " + deviceId);
            return deviceId;
        }

        public void LogEvent(string name, string properties)
        {
            TapLog.Log("TapEventMobile LogEvent" + name + properties);
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("logEvent")
                .Args("name", name)
                .Args("properties", properties)
                .CommandBuilder());
        }

        public void DeviceInitialize(string properties)
        {
            TapLog.Log("TapEventMobile DeviceInitialize" + properties);
#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("deviceInitialize")
                .Args("deviceInitialize", properties)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("deviceInitialize")
                .Args("properties", properties)
                .CommandBuilder());
#endif
        }

        public void DeviceUpdate(string properties)
        {
            TapLog.Log("TapEventMobile DeviceUpdate" + properties);
#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("deviceUpdate")
                .Args("deviceUpdate", properties)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("deviceUpdate")
                .Args("properties", properties)
                .CommandBuilder());
#endif
        }

        public void DeviceAdd(string properties)
        {
            TapLog.Log("TapEventMobile DeviceAdd" + properties);
#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("deviceAdd")
                .Args("deviceAdd", properties)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("deviceAdd")
                .Args("properties", properties)
                .CommandBuilder());
#endif
        }

        public void UserInitialize(string properties)
        {
            TapLog.Log("TapEventMobile UserInitialize" + properties);
#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("userInitialize")
                .Args("userInitialize", properties)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("userInitialize")
                .Args("properties", properties)
                .CommandBuilder());
#endif
        }

        public void UserUpdate(string properties)
        {
            TapLog.Log("TapEventMobile UserUpdate" + properties);
#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("userUpdate")
                .Args("userUpdate", properties)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("userUpdate")
                .Args("properties", properties)
                .CommandBuilder());
#endif
        }

        public void UserAdd(string properties)
        {
            TapLog.Log("TapEventMobile UserAdd" + properties);
#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("userAdd")
                .Args("userAdd", properties)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("userAdd")
                .Args("properties", properties)
                .CommandBuilder());
#endif
        }

        public void AddCommonProperty(string key, string value)
        {
            TapLog.Log("TapEventMobile AddCommonProperty" + key + value);
#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("addCommonProperty")
                .Args("addCommonProperty", key)
                .Args("value", value)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("addCommonProperty")
                .Args("key", key)
                .Args("value", value)
                .CommandBuilder());
#endif
        }

        public void AddCommon(string properties)
        {
            TapLog.Log("TapEventMobile AddCommon" + properties);
#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("addCommon")
                .Args("addCommon", properties)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("addCommonProperties")
                .Args("properties", properties)
                .CommandBuilder());
#endif
        }

        public void ClearCommonProperty(string key)
        {
            TapLog.Log("TapEventMobile ClearCommonProperty");

#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("clearCommonProperty")
                .Args("clearCommonProperty", key)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("clearCommonProperty")
                .Args("key", key)
                .CommandBuilder());
#endif
        }

        public void ClearCommonProperties(string[] keys)
        {
            TapLog.Log("TapEventMobile ClearCommonProperties");

#if UNITY_IOS
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("clearCommonProperties")
                .Args("clearCommonProperties", keys)
                .CommandBuilder());
#else
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("clearCommonProperties")
                .Args("keys", keys)
                .CommandBuilder());
#endif
        }

        public void ClearAllCommonProperties()
        {
            TapLog.Log("TapEventMobile ClearAllCommonProperties");

            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("clearAllCommonProperties")
                .CommandBuilder());
        }

        public void LogChargeEvent(string orderID, string productName, long amount, string currencyType, string paymentMethod, string properties)
        {
            TapLog.Log("TapEventMobile LogChargeEvent" + orderID);

            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("logPurchasedEvent")
                .Args("orderID", orderID)
                .Args("productName", productName)
                .Args("amount", amount)
                .Args("currencyType", currencyType)
                .Args("paymentMethod", paymentMethod)
                .Args("properties", properties)
                .CommandBuilder());
        }

        public void RegisterDynamicProperties(Func<string> callback)
        {
            TapLog.Log("RegisterDynamicProperties called" + callback);
#if UNITY_IOS
            IOSNativeWrapper.RegisterDynamicProperties(callback);
#else
            AndroidNativeWrapper.RegisterDynamicProperties(callback);
#endif
        }

        public void SetOAID(string value)
        {
            TapLog.Log("TapEventMobile SetOAID" + value);
#if UNITY_ANDROID
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("setOAID")
                .Args("oaid", value)
                .CommandBuilder());            
#endif
        }

        public void LogDeviceLoginEvent()
        {
            TapLog.Log("TapEventMobile LogDeviceLoginEvent");
#if UNITY_ANDROID
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("logDeviceLoginEvent")
                .CommandBuilder());            
#endif
        }
    }
}