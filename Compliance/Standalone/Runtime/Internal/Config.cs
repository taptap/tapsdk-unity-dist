using System;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using TapSDK.Compliance.Model;
using TapSDK.Core;

namespace TapSDK.Compliance.Internal 
{
    public static class Config 
    {
        internal static readonly string ANTI_ADDICTION_DIR = "tap-anti-addiction";
        static readonly string CONFIG_FILENAME = "realname_config";
        static readonly string USER_CONFIG_FILENAME = "user_config";

        private const string TIME_FROMAT = "HH:mm";

        internal static readonly Lazy<Persistence> persistence = new Lazy<Persistence>(() => new Persistence(CONFIG_FILENAME));
        internal static readonly Lazy<Persistence> userPersistence = new Lazy<Persistence>(() =>new Persistence(USER_CONFIG_FILENAME));
        

        static RealNameConfigResult current;

        public static Local userLocalConfig;

        public static RealNameConfigResult Current 
        {
            get => current;
            private set =>  current = value;
        }

        private static RealNameConfigResult _localConfig;

        internal static async Task<bool> Fetch(string userId) 
        {
            while(true){
                //从服务端加载
                TapLogger.Debug("start feat global config from server");
                try 
                {
                    Current = await Network.FetchConfig(userId);
                    if (IsValid())
                    {
                        await persistence.Value.Save(Current);
                        return true;
                    }
                } 
                catch (Exception e) 
                {
                    TapLogger.Error(e);
                    if (e is ComplianceException aee && aee.code < 500)
                    {
                        return false;
                    }
                }

                TapLogger.Debug("start feat global config from local");
        
                // 从设备缓存加载
                try 
                {
                    Current = await persistence.Value.Load<RealNameConfigResult>();
                    if (Current != null)
                        return true;
                } 
                catch (Exception e) 
                {
                    TapLogger.Error(e);
                }
                //使用本地默认
                // _localConfig = LoadFromBuiltin(); 
                // Current = _localConfig;
                return false;
            }
        }
        

        private static bool IsValid()
        {
            if (current == null) return false;
            if (current.realNameText == null) return false;
            return true;
        }


        
        internal static bool NeedUploadUserAction => true;

        public static HealthReminderDesc GetMinorUnplayableHealthReminderTip() 
        {
            return userLocalConfig.timeRangeConfig.uITipText.reject;

        }

    
        internal static Prompt GetInputIdentifyTip()
        {
            return Current.realNameText.manualAuthTip;
        }
        internal static Prompt GetQuickVerifyTipPanelTip()
        {
            return Current.realNameText.tapAuthTip;
        }
        
        public static Prompt GetInputIdentifyFormatErrorTip()
        {
            return Current.realNameText.manualAuthFailedTip;
        }
        
        /// <summary>
        /// 认证中提示(因为中宣部认证无响应)
        /// </summary>
        /// <returns></returns>
        public static Prompt GetInputIdentifyBlockingTip()
        {
           return Current.realNameText.authWaitingTip;
        }
        
        public static HealthReminderDesc GetMinorPlayableHealthReminderTip() 
        {
            return userLocalConfig.timeRangeConfig.uITipText.allow;
        }

        public static DateTimeOffset StrictStartTime =>
            DateTimeOffset.ParseExact(userLocalConfig.timeRangeConfig.timeStart,
                TIME_FROMAT, CultureInfo.InvariantCulture);

        public static DateTimeOffset StrictEndTime =>
            DateTimeOffset.ParseExact(userLocalConfig.timeRangeConfig.timeEnd,
                TIME_FROMAT, CultureInfo.InvariantCulture);
    }
}
