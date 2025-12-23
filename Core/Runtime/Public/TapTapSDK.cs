using System;
using System.Threading.Tasks;
using System.Linq;
using TapSDK.Core.Internal;
using System.Collections.Generic;

using UnityEngine;
using System.Reflection;
using TapSDK.Core.Internal.Init;
using TapSDK.Core.Internal.Log;
using System.ComponentModel;

namespace TapSDK.Core {
    public class TapTapSDK {
        public static readonly string Version = "4.9.3";
        
        public static string SDKPlatform = "TapSDK-Unity";

        public static TapTapSdkOptions taptapSdkOptions;

        private static ITapCorePlatform platformWrapper;

        private static bool disableDurationStatistics;

        public static bool DisableDurationStatistics {
            get => disableDurationStatistics;
            set {
                disableDurationStatistics = value;
            }
        }

        static TapTapSDK() {
            platformWrapper = PlatformTypeUtils.CreatePlatformImplementationObject(typeof(ITapCorePlatform),
                "TapSDK.Core") as ITapCorePlatform;
        }

        public static void Init(TapTapSdkOptions coreOption)
        {
            if (coreOption == null)
                throw new ArgumentException("[TapSDK] options is null!");
            TapTapSDK.taptapSdkOptions = coreOption;
            TapLog.Enabled = coreOption.enableLog;
            platformWrapper?.Init(coreOption);
            // 初始化各个模块

            Type[] initTaskTypes = GetInitTypeList();
            if (initTaskTypes != null)
            {
                List<IInitTask> initTasks = new List<IInitTask>();
                foreach (Type initTaskType in initTaskTypes)
                {
                    initTasks.Add(Activator.CreateInstance(initTaskType) as IInitTask);
                }
                initTasks = initTasks.OrderBy(task => task.Order).ToList();
                foreach (IInitTask task in initTasks)
                {
                    TapLogger.Debug($"Init: {task.GetType().Name}");
                    task.Init(coreOption);
                }
            }
            TapTapEvent.Init(HandleEventOptions(coreOption));

        }

        public static void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            if (coreOption == null)
                throw new ArgumentException("[TapSDK] options is null!");

            TapTapSDK.taptapSdkOptions = coreOption;
            TapLog.Enabled = coreOption.enableLog;
            platformWrapper?.Init(coreOption, otherOptions);


            Type[] initTaskTypes = GetInitTypeList();
            if (initTaskTypes != null)
            {
                List<IInitTask> initTasks = new List<IInitTask>();
                foreach (Type initTaskType in initTaskTypes)
                {
                    initTasks.Add(Activator.CreateInstance(initTaskType) as IInitTask);
                }
                initTasks = initTasks.OrderBy(task => task.Order).ToList();
                foreach (IInitTask task in initTasks)
                {
                    TapLog.Log($"Init: {task.GetType().Name}");
                    task.Init(coreOption, otherOptions);
                }
            }
            TapTapEvent.Init(HandleEventOptions(coreOption, otherOptions));
        }

        /// <summary>
        /// 通过初始化属性设置 TapEvent 属性，兼容旧版本
        /// </summary>
        /// <param name="coreOption"></param>
        /// <param name="otherOptions"></param>
        /// <returns>TapEvent 属性</returns>
        private static TapTapEventOptions HandleEventOptions(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions = null)
        {
            TapTapEventOptions tapEventOptions = null;
            if (otherOptions != null && otherOptions.Length > 0)
            {
                foreach (TapTapSdkBaseOptions otherOption in otherOptions)
                {
                    if (otherOption is TapTapEventOptions option)
                    {
                        tapEventOptions = option;
                    }
                }
            }
            if (tapEventOptions == null)
            {
                tapEventOptions = new TapTapEventOptions();
                if (coreOption != null)
                {
                    tapEventOptions.channel = coreOption.channel;
                    tapEventOptions.disableAutoLogDeviceLogin = coreOption.disableAutoLogDeviceLogin;
                    tapEventOptions.enableAutoIAPEvent = coreOption.enableAutoIAPEvent;
                    tapEventOptions.overrideBuiltInParameters = coreOption.overrideBuiltInParameters;
                    tapEventOptions.propertiesJson = coreOption.propertiesJson;
                }
            }
            return tapEventOptions;
        }

        // UpdateLanguage 方法
        public static void UpdateLanguage(TapTapLanguageType language)
        {
            platformWrapper?.UpdateLanguage(language);
        }
        
        // 是否通过 PC 启动器唤起游戏
        public static Task<bool> IsLaunchedFromTapTapPC()
        {
            return platformWrapper?.IsLaunchedFromTapTapPC();
        }

        private static Type[] GetInitTypeList(){
            Type interfaceType = typeof(IInitTask);
            Type[] initTaskTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asssembly => asssembly.GetName().FullName.StartsWith("TapSDK"))
                .SelectMany(assembly => assembly.GetTypes())
                .Where(clazz => interfaceType.IsAssignableFrom(clazz) && clazz.IsClass)
                .ToArray();
            return initTaskTypes;
        }

    }
}
