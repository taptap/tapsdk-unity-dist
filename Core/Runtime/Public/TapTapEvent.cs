using System;
using System.Threading.Tasks;
using TapSDK.Core.Internal;
using UnityEngine;
using System.Reflection;

namespace TapSDK.Core {
    public class TapTapEvent {
       
        private static ITapEventPlatform platformWrapper;

       
        static TapTapEvent() {
            platformWrapper = PlatformTypeUtils.CreatePlatformImplementationObject(typeof(ITapEventPlatform),
                "TapSDK.Core") as ITapEventPlatform;
            if(platformWrapper == null) {
                Debug.LogError("PlatformWrapper is null");
            }
        }

        internal static void Init(TapTapEventOptions eventOptions)
        {
            platformWrapper.Init(eventOptions);
        }

        public static void SetUserID(string userID)
        {
            platformWrapper?.SetUserID(userID);
        }
        
        public static void SetUserID(string userID, string properties){
            platformWrapper?.SetUserID(userID,properties);
        }
        public static void ClearUser(){
            platformWrapper?.ClearUser();
        }

        public static string GetDeviceId(){
            if(platformWrapper != null) {
                return platformWrapper?.GetDeviceId();
            }
            return "";
        }
    
        public static void LogEvent(string name, string properties){
            platformWrapper?.LogEvent(name, properties);
        }

        public static void DeviceInitialize(string properties){
            platformWrapper?.DeviceInitialize(properties);
        }

        public static void DeviceUpdate(string properties){
            platformWrapper?.DeviceUpdate(properties);
        }
        public static void DeviceAdd(string properties){
            platformWrapper?.DeviceAdd(properties);
        }

        public static void UserInitialize(string properties){
            platformWrapper?.UserInitialize(properties);
        }

        public static void UserUpdate(string properties){
            platformWrapper?.UserUpdate(properties);
        }

        public static void UserAdd(string properties){
            platformWrapper?.UserAdd(properties);
        }

        public static void AddCommonProperty(string key, string value){
            platformWrapper?.AddCommonProperty(key, value);
        }

        public static void AddCommon(string properties){
            platformWrapper?.AddCommon(properties);
        }

        public static void ClearCommonProperty(string key){
            platformWrapper?.ClearCommonProperty(key);
        }
        public static void ClearCommonProperties(string[] keys){
            platformWrapper?.ClearCommonProperties(keys);
        }

        public static void ClearAllCommonProperties(){
            platformWrapper?.ClearAllCommonProperties();
        }
        public static void LogPurchasedEvent(string orderID, string productName, long amount, string currencyType, string paymentMethod, string properties){
            platformWrapper?.LogChargeEvent(orderID, productName, amount, currencyType,  paymentMethod, properties);
        }
    
        public static void RegisterDynamicProperties(Func<string> callback){
            platformWrapper?.RegisterDynamicProperties(callback);
        }

        public static void SetOAID(string value){
            platformWrapper?.SetOAID(value);
        }
        
        public static void LogDeviceLoginEvent(){
            platformWrapper?.LogDeviceLoginEvent();
        }
    }
}
