using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using AOT;
using Newtonsoft.Json;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone.Internal.Http;
using UnityEngine;
#if UNITY_STANDALONE
using static TapSDK.Core.Standalone.Internal.Openlog.TapOpenlogWrapper;
#endif

namespace TapSDK.Core.Standalone.Internal.Openlog
{
    public class TapOpenlogStandalone
    {
        public static string openid = "";
        private static readonly Dictionary<string, string> generalParameter = new Dictionary<string, string>();
        private static readonly Dictionary<string, System.Object> openlogStartParameter = new Dictionary<string, System.Object>();
        private readonly string sdkProjectName;
        private readonly string sdkProjectVersion;
        private static readonly bool isRND = false;
        private static readonly TapLog log = new TapLog(module: "Openlog");
#if UNITY_STANDALONE
        private static readonly CommonVariablesGetter commonVariablesGetter = new CommonVariablesGetter(GetCommonVariables);
        private static readonly FreeStringCallback freeString = new FreeStringCallback(FreeString);
#endif
        public static void Init()
        {
#if UNITY_STANDALONE
            InitGeneralParameter();
            InitOpenlogStartParameter();
            string openlogStartStr = JsonConvert.SerializeObject(openlogStartParameter);
            int result = TdkOnAppStarted(openlogStartStr, commonVariablesGetter, freeString);
            BindWindowChange();
#endif
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static IntPtr GetCommonVariables()
        {
            Dictionary<string, string> dynamicProperties = InflateDynamicProperties();
            string jsonStr = JsonConvert.SerializeObject(dynamicProperties);
            return Marshal.StringToHGlobalAnsi(jsonStr);
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static void FreeString(IntPtr intPtr)
        {
            if (intPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(intPtr);
            }
        }

        private static void BindWindowChange()
        {
#if UNITY_STANDALONE
            EventManager.AddListener(EventManager.OnApplicationPause, (isPause) =>
            {
                var isPauseBool = (bool)isPause;
                if (isPauseBool)
                {
                    TdkOnBackground();
                }
                else
                {
                    TdkOnForeground();
                }
            });

            EventManager.AddListener(EventManager.OnApplicationQuit, (quit) =>
            {
                TdkOnAppStopped();
            });
            EventManager.AddListener(EventManager.OnComplianceUserChanged, (userInfo) => 
            {
                TdkSetExtraAppDurationParams(userInfo.ToString());
            });
#endif            
        }

        public TapOpenlogStandalone(string sdkProjectName, string sdkProjectVersion)
        {
            this.sdkProjectName = sdkProjectName;
            this.sdkProjectVersion = sdkProjectVersion;
        }

        public void LogBusiness(
            string action,
            Dictionary<string, string> properties = null
        )
        {
#if UNITY_STANDALONE
            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }
            properties[TapOpenlogParamConstants.PARAM_TAPSDK_PROJECT] = sdkProjectName;
            properties[TapOpenlogParamConstants.PARAM_TAPSDK_VERSION] = sdkProjectVersion;
            properties[TapOpenlogParamConstants.PARAM_ACTION] = action;
            string propertiesStr = JsonConvert.SerializeObject(properties);
            TdkOpenLog("tapsdk", propertiesStr);
#endif
        }

        public void LogTechnology(
            string action,
            Dictionary<string, string> properties = null
        )
        {
#if UNITY_STANDALONE
            if (TapCoreStandalone.coreOptions.region == TapTapRegionType.CN && !"TapPayment".Equals(sdkProjectName))
            {
                // 国内非支付SDK不上报技术日志
                return;
            }
            Dictionary<string, string> content = InflateDynamicProperties();
            content[TapOpenlogParamConstants.PARAM_TAPSDK_PROJECT] = sdkProjectName;
            content[TapOpenlogParamConstants.PARAM_TAPSDK_VERSION] = sdkProjectVersion;
            content[TapOpenlogParamConstants.PARAM_ACTION] = action;
            if (properties != null)
            {
                content["args"] = JsonConvert.SerializeObject(properties);
            }
            string propertiesStr = JsonConvert.SerializeObject(content);
            TdkOpenLog("tapsdk-apm", propertiesStr);
#endif
        }

        private static void InitOpenlogStartParameter()
        {
            if (isRND)
            {
                openlogStartParameter[TapOpenlogStartParamConstants.PARAM_REGION] = 2;
            }
            else
            {
                openlogStartParameter[TapOpenlogStartParamConstants.PARAM_REGION] = TapCoreStandalone.coreOptions.region;
            }
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_LOG_TO_CONSOLE] = 1;
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_LOG_LEVEL] = 1;
            string openLogDirPath = Path.Combine(Application.persistentDataPath, "OpenlogData");
            if (TapTapSDK.taptapSdkOptions != null && !string.IsNullOrEmpty(TapTapSDK.taptapSdkOptions.clientId)){
                openLogDirPath = Path.Combine(Application.persistentDataPath, "OpenlogData_" + TapTapSDK.taptapSdkOptions.clientId);
                if( !Directory.Exists(openLogDirPath)){
                    string oldPath = Path.Combine(Application.persistentDataPath, "OpenlogData");
                    if(Directory.Exists(oldPath)){
                        Directory.Move(oldPath, openLogDirPath);
                    }
                }
            }
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_DATA_DIR] = openLogDirPath;
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_ENV] = "local";
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_PLATFORM] = "PC";
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_UA] = TapHttpUtils.GenerateUserAgent();
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_CLIENT_ID] = TapCoreStandalone.coreOptions.clientId;
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_CLIENT_TOKEN] = TapCoreStandalone.coreOptions.clientToken;
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_COMMON] = generalParameter;
            openlogStartParameter[TapOpenlogStartParamConstants.PARAM_APP_DURATION] = new Dictionary<string, string>()
            {
                {TapOpenlogParamConstants.PARAM_TAPSDK_VERSION, TapTapSDK.Version}
            };
        }

        private static void InitGeneralParameter()
        {
            // 应用包名
            generalParameter[TapOpenlogParamConstants.PARAM_APP_PACKAGE_NAME] = Application.identifier;
            // 应用版本字符串
            generalParameter[TapOpenlogParamConstants.PARAM_APP_VERSION] = Application.version;
            // 应用版本（数字）
            generalParameter[TapOpenlogParamConstants.PARAM_APP_VERSION_CODE] = "";
            // 固定一个枚举值: TapSDK
            generalParameter[TapOpenlogParamConstants.PARAM_PN] = "TapSDK";
            // SDK生成的设备唯一标识
            generalParameter[TapOpenlogParamConstants.PARAM_DEVICE_ID] = SystemInfo.deviceUniqueIdentifier;
            // SDK生成的设备一次安装的唯一标识
            generalParameter[TapOpenlogParamConstants.PARAM_INSTALL_UUID] = Identity.InstallationId;
            // 设备品牌，eg: Xiaomi
            generalParameter[TapOpenlogParamConstants.PARAM_DV] = "";
            // 设备品牌型号，eg：21051182C
            generalParameter[TapOpenlogParamConstants.PARAM_MD] = SystemInfo.deviceModel;
            // 设备CPU型号，eg：arm64-v8a
            generalParameter[TapOpenlogParamConstants.PARAM_CPU] = "";
            // 支持 CPU 架构，eg：arm64-v8a
            generalParameter[TapOpenlogParamConstants.PARAM_CPU_ABIS] = "";
            // 设备操作系统
            generalParameter[TapOpenlogParamConstants.PARAM_OS] = SystemInfo.operatingSystemFamily.ToString();
            // 设备操作系统版本
            generalParameter[TapOpenlogParamConstants.PARAM_SV] = SystemInfo.operatingSystem;
            // 物理设备真实屏幕分辨率宽
            generalParameter[TapOpenlogParamConstants.PARAM_WIDTH] = Screen.currentResolution.width.ToString();
            // 物理设备真实屏幕分辨率高
            generalParameter[TapOpenlogParamConstants.PARAM_HEIGHT] = Screen.currentResolution.height.ToString();
            // 设备总存储空间（磁盘），单位B
            generalParameter[TapOpenlogParamConstants.PARAM_TOTAL_ROM] = "";
            // 设备总内存，单位B
            generalParameter[TapOpenlogParamConstants.PARAM_TOTAL_RAM] = DeviceInfo.RAM;
            // 芯片型号，eg：Qualcomm Technologies, Inc SM7250
            generalParameter[TapOpenlogParamConstants.PARAM_HARDWARE] = SystemInfo.processorType;
            // SDK设置的地区，例如 zh_CN
            generalParameter[TapOpenlogParamConstants.PARAM_SDK_LOCALE] = Tracker.getServerLanguage();
            // taptap的用户ID的外显ID（加密）
            generalParameter[TapOpenlogParamConstants.PARAM_OPEN_ID] = openid;
        }

        private static Dictionary<string, string> InflateDynamicProperties()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            // 客户端时区，eg：Asia/Shanghai
            props[TapOpenlogParamConstants.PARAM_TIMEZONE] = "";
            // SDK 产物类型
            props[TapOpenlogParamConstants.PARAM_TAPSDK_ARTIFACT] = "Unity";
            // 游戏账号 ID（非角色 ID）
            props[TapOpenlogParamConstants.PARAM_GAME_USER_ID] = TapCoreStandalone.User.Id ?? "";
            // taptap的用户ID的外显ID（加密）
            props[TapOpenlogParamConstants.PARAM_OPEN_ID] = openid ?? "";
            // SDK生成的设备全局唯一标识
            props[TapOpenlogParamConstants.PARAM_GID] = "";
            // 设备可用存储空间（磁盘），单位B
            props[TapOpenlogParamConstants.PARAM_ROM] = "0";
            // 设备可用内存，单位B
            props[TapOpenlogParamConstants.PARAM_RAM] = "0";
            // 网络类型，eg：wifi, mobile
            props[TapOpenlogParamConstants.PARAM_NETWORK_TYPE] = "";
            // SDK设置的地区，例如 zh_CN
            props[TapOpenlogParamConstants.PARAM_SDK_LOCALE] = Tracker.getServerLanguage();
            return props;
        }
    }
}