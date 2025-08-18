
namespace TapSDK.Core.Standalone.Internal {
    public static class Constants {
        public static readonly string EVENT = "event";

        public static readonly string PROPERTY_INITIALIZE_TYPE = "initialise";
        public static readonly string PROPERTY_UPDATE_TYPE = "update";
        public static readonly string PROPERTY_ADD_TYPE = "add";


        public readonly static string SERVER_URL_CN = "https://e.tapdb.net";
        public readonly static string SERVER_URL_IO = "https://e.tapdb.ap-sg.tapapis.com";
        public readonly static string DEVICE_LOGIN = "device_login";
        public readonly static string USER_LOGIN = "user_login";

        internal static string ClientSettingsFileName = "TapSDKClientSettings";
        internal static string ClientSettingsEventKey = "ClientSettingsEventKey";
    }

}
