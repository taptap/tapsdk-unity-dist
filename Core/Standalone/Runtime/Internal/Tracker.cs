using System;
using System.Collections.Generic;
using TapSDK.Core.Standalone;
using System.Threading.Tasks;
using UnityEngine;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Core.Standalone.Internal {
    public class Tracker {

        private Dictionary<string, object> customProps;

        private Dictionary<string, object> basicProps;
        private Dictionary<string, object> commonProps;

        private EventSender sender;
        private IDynamicProperties dynamicPropsDelegate;

        private static string session_uuid = generateUUID();

        public void Init() {
            basicProps = new Dictionary<string, object>();
            commonProps = new Dictionary<string, object>();

            var coreOptions = TapCoreStandalone.coreOptions;
            customProps = Json.Deserialize(coreOptions.propertiesJson) as Dictionary<string, object>;
            sender = new EventSender();
            
            InitBasicProps();
            
            Dictionary<string, object> props = new Dictionary<string, object>(basicProps);
            TrackEvent(Constants.DEVICE_LOGIN, props, true);
            
        }

        public void AddCommonProperty(string key, object value) {
            commonProps[key] = value;
        }

        public void AddCommon(Dictionary<string, object> properties) {
            foreach (KeyValuePair<string, object> kv in properties) {
                commonProps[kv.Key] = kv.Value;
            }
        }
        public void ClearCommonProperty(string key) {
            commonProps.Remove(key);
        }
        public void ClearCommonProperties(string[] keys) {
            foreach (string key in keys) {
                commonProps.Remove(key);
            }
        }
        public void ClearAllCommonProperties() {
            commonProps.Clear();
        }

        public void RegisterDynamicPropsDelegate(IDynamicProperties dynamicPropsDelegate) {
            this.dynamicPropsDelegate = dynamicPropsDelegate;
        }

        public void LogPurchasedEvent(string orderID, string productName, Int64 amount, string currencyType, string paymentMethod, string properties){
            var prop = Json.Deserialize(properties) as Dictionary<string, object>;
            
            var data = new Dictionary<string, object> {
                { "order_id", orderID },
                { "product", productName },
                { "amount", amount },
                { "currency_type", currencyType },
                { "payment", paymentMethod }
            };
            if (prop != null) {
                foreach (KeyValuePair<string, object> kv in prop) {
                    data[kv.Key] = kv.Value;
                }
            }
            TrackEvent("charge", data);
        }

        /// <summary>
        /// 上报事件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="properties"></param>
        /// <param name="isAutomationlly">是否为自动事件</param>
        public void TrackEvent(string name, Dictionary<string, object> properties = null, bool isAutomationlly = false) {

            Dictionary<string, object> props = new Dictionary<string, object>(basicProps);

            if (commonProps != null) {
                foreach (KeyValuePair<string, object> kv in commonProps) {
                    props[kv.Key] = kv.Value;
                }
            }

            Dictionary<string, object> dynamicProps = dynamicPropsDelegate?.GetDynamicProperties();
            TapLog.Log("dynamicProps: " + dynamicProps);
            if (dynamicProps != null) {
                foreach (KeyValuePair<string, object> kv in dynamicProps) {
                    props[kv.Key] = kv.Value;
                }
            }

            if (name == Constants.DEVICE_LOGIN) { // Device login 事件带上初始化时的自定义属性
                TapLog.Log("customProps: " + customProps);
                if (customProps != null) {
                    foreach (KeyValuePair<string, object> kv in customProps) {
                        props[kv.Key] = kv.Value;
                    }
                }
            }

            props["t_log_id"] = generateUUID();
            // 时间戳，毫秒级
            props["timestamp"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var open_id = OpenID;
            if (!string.IsNullOrWhiteSpace(open_id)) {
                props["open_id"] = open_id;
            }

            TapLog.Log("properties: " + properties);
            if (properties != null) {
                foreach (KeyValuePair<string, object> kv in properties) {
                    props[kv.Key] = kv.Value;
                }
            }

            props["is_automatically_log"] = isAutomationlly ? "true" : "false";

            var language = getServerLanguage();
            props["sdk_locale"] = language;
            props["lang_system"] = DeviceInfo.GetLanguage();

            Dictionary<string, object> data = new Dictionary<string, object> {
                { "client_id", TapCoreStandalone.coreOptions.clientId },
                { "type", "track" },
                { "name", name },
                { "device_id", Identity.DeviceId },
                { "properties", props },
            };
            if (!string.IsNullOrWhiteSpace(TapCoreStandalone.User.Id)) {
                data["user_id"] = TapCoreStandalone.User.Id;
            }

            sender.Send(data);
        }

        /// <summary>
        /// 上报设备属性变化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="properties"></param>
        public void TrackDeviceProperties(string type, Dictionary<string, object> properties) {
            if (string.IsNullOrWhiteSpace(Identity.DeviceId)) {
                TapLog.Error("DeviceId is NULL.");
                return;
            }

            Dictionary<string, object> baseProps = new Dictionary<string, object> {
                { "device_id", Identity.DeviceId }
            };
            _ = TrackPropertiesAsync(type, baseProps, properties);
        }

        /// <summary>
        /// 上报玩家属性变化
        /// </summary>
        public void TrackUserProperties(string type, Dictionary<string, object> properties) {
            string userId = TapCoreStandalone.User.Id;
            if (string.IsNullOrWhiteSpace(userId)) {
                TapLog.Error("UserId is NULL.");
                return;
            }

            Dictionary<string, object> baseProps = new Dictionary<string, object> {
                { "user_id", userId }
            };
            _ = TrackPropertiesAsync(type, baseProps, properties);
        }

        private Task TrackPropertiesAsync(string type,
            Dictionary<string, object> basicProps, Dictionary<string, object> properties)
        {
            if (!IsInitialized) {
                return Task.CompletedTask;
            }

            if (properties == null) {
                properties = new Dictionary<string, object>();
            }
            properties["sdk_version"] = TapTapSDK.Version;

            Dictionary<string, object> data = new Dictionary<string, object>(basicProps) {
                { "client_id", TapCoreStandalone.coreOptions.clientId },
                { "type", type },
                { "properties", properties }
            };

            sender.Send(data);
            return Task.CompletedTask;
        }

        private void InitBasicProps() {
            DeviceInfo.GetMacAddress(out string macList, out string firstMac);
            basicProps = new Dictionary<string, object> {
                { "os", OS },
                { "md", SystemInfo.deviceModel },
                { "sv", SystemInfo.operatingSystem },
                { "pn", "TapSDK" },
                { "tapsdk_project", "TapSDKCore" },
                { "session_uuid", session_uuid },
                { "install_uuid", Identity.InstallationId },
                { "persist_uuid", Identity.PersistentId },
                { "ram", DeviceInfo.RAM },
                { "rom", "0" },
                { "width", Screen.currentResolution.width },
                { "height", Screen.currentResolution.height },
                { "provider", "unknown" },
                { "app_version", TapCoreStandalone.coreOptions.gameVersion ?? Application.version },
                { "sdk_version", TapTapSDK.Version },
                { "network_type", Network },
                { "channel", TapCoreStandalone.coreOptions.channel },
                { "mac_list", macList },
                { "first_mac", firstMac },
                { "device_id5", DeviceInfo.GetLaunchUniqueID() }
            };
        }
        private string OS {
            get {
                switch (SystemInfo.operatingSystemFamily) {
                    case OperatingSystemFamily.Windows:
                        return "Windows";
                    case OperatingSystemFamily.MacOSX:
                        return "Mac";
                    case OperatingSystemFamily.Linux:
                        return "Linux";
                    default:
                        return "Unknown";
                }
            }
        }

        public static string getServerLanguage() {
            // 将 TapCoreStandalone.coreOptions.preferredLanguage 转成 zh_TW/en/zh_CN/en_GB/jp/fil 等格式
            switch (TapCoreStandalone.coreOptions.preferredLanguage) {
                case TapTapLanguageType.zh_Hans:
                    return "zh_CN";
                case TapTapLanguageType.zh_Hant:
                    return "zh_TW";
                case TapTapLanguageType.en:
                    return "en_US";
                case TapTapLanguageType.ja:
                    return "ja_JP";
                case TapTapLanguageType.ko:
                    return "ko_KR";
                case TapTapLanguageType.th:
                    return "th_TH";
                case TapTapLanguageType.id:
                    return "id_ID";
                case TapTapLanguageType.de:
                    return "de";
                case TapTapLanguageType.es:
                    return "es_ES";
                case TapTapLanguageType.fr:
                    return "fr";
                case TapTapLanguageType.pt:
                    return "pt_PT";
                case TapTapLanguageType.ru:
                    return "ru";
                case TapTapLanguageType.tr:
                    return "tr";
                case TapTapLanguageType.vi:
                    return "vi_VN";
                default:
                // 默认cn返回简体中文，Overseas返回英文
                    return TapCoreStandalone.coreOptions.region == TapTapRegionType.CN ? "zh_CN" : "en_US";
            }
        }

        private string Network {
            get {
                switch (Application.internetReachability) {
                    case NetworkReachability.ReachableViaCarrierDataNetwork:
                        return "3";
                    case NetworkReachability.ReachableViaLocalAreaNetwork:
                        return "2";
                    default:
                        return "Unknown";
                }
            }
        }

        private bool IsInitialized {
            get {
                if (string.IsNullOrWhiteSpace(TapCoreStandalone.coreOptions.clientId)) {
                    TapLog.Error("MUST be initialized.");
                    return false;
                }
                return true;
            }
        }

        private static string generateUUID() {
            return Guid.NewGuid().ToString();
        }

        private static string OpenID {
            get {
                IOpenIDProvider provider = BridgeUtils.CreateBridgeImplementation(typeof(IOpenIDProvider),
                    "TapSDK.Login") as IOpenIDProvider;
                return provider?.GetOpenID();
            }
        }

        public interface IDynamicProperties {
            Dictionary<string, object> GetDynamicProperties();
        }
    }
}