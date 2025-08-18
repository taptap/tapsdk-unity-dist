using System;

namespace TapSDK.Core.Internal {
    public interface ITapEventPlatform {
        
        void Init(TapTapEventOptions eventOptions);

        void SetUserID(string userID);
        
        void SetUserID(string userID, string properties);
        void ClearUser();

        string GetDeviceId();
    
        void LogEvent(string name, string properties);

        void DeviceInitialize(string properties);

        void DeviceUpdate(string properties);
        void DeviceAdd(string properties);

        void UserInitialize(string properties);

        void UserUpdate(string properties);

        void UserAdd(string properties);

        void AddCommonProperty(string key, string value);

        void AddCommon(string properties);

        void ClearCommonProperty(string key);
        void ClearCommonProperties(string[] keys);

        void ClearAllCommonProperties();
        void LogChargeEvent(string orderID, string productName, long amount, string currencyType, string paymentMethod, string properties);
    
        void RegisterDynamicProperties(Func<string> callback);

        void SetOAID(string value);
        
        void LogDeviceLoginEvent();
    }
}