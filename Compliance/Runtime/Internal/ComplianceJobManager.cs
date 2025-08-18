using System;
using System.Linq;
using UnityEngine;
using TapSDK.Core;
using TapSDK.Compliance.Model;
using System.Collections.Generic;

namespace TapSDK.Compliance.Internal
{
 
    internal static class ComplianceJobManager
    {
        
        private static IComplianceJob _job;

        internal static IComplianceJob Job
        {
            get
            {
                if (_job == null)
                {
                    InitJob();
                }
                return _job;
            }
        }
        
        private static bool _isInit = false;
        
        public static List<Action<int, string>> ExternalCallbackList
        {
            get => Job?.ExternalCallbackList;
        }
        
        private static IComplianceJob CreateJob(bool isNewJob)
        {
            if (isNewJob)
            {
                var result = Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblies()
                    .Where(asssembly => asssembly.GetName().FullName.StartsWith("TapSDK.Compliance"))
                    .SelectMany(assembly => assembly.GetTypes())
                    .SingleOrDefault((clazz) => typeof(IComplianceJob).IsAssignableFrom(clazz) && clazz.IsClass 
                    && clazz.Name.Contains("ComplianceNewJob")));
                return result as IComplianceJob;
            }
            else
            {
                var result = Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblies()
                    .Where(asssembly => asssembly.GetName().FullName.StartsWith("TapSDK.Compliance.Mobile.Runtime"))
                    .SelectMany(assembly => assembly.GetTypes())
                    .SingleOrDefault((clazz) => typeof(IComplianceJob).IsAssignableFrom(clazz) && clazz.IsClass 
                        && clazz.Name.Contains("ComplianceMobileOldJob")));
                return result as IComplianceJob;
            }
        }

        private static void InitJob()
        {
            // 国内-移动端防沉迷用桥接的方式
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                _job = CreateJob(false);
            }
            // 其他均使用 Unity Native 的方式
            else
            {
                _job = CreateJob(true);
            }
            
            TapLogger.Debug(string.Format("Anti Addiction Job Type: {0} ! Platform: {1}", _job.GetType(),  Application.platform.ToString()));
        }

        internal static void Init(string clientId, string clientToken, TapTapRegionType regionType, TapTapComplianceOption config)
        {
            Job.Init(clientId, clientToken, regionType, config);
            _isInit = true;
        }

        internal static bool IsInit(){
          return _isInit;
        }

      
    }
}