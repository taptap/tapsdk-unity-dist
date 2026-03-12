using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Core.Internal.Utils {
    public static class BridgeUtils {
        public static bool IsSupportMobilePlatform => Application.platform == RuntimePlatform.Android ||
            Application.platform == RuntimePlatform.IPhonePlayer;

        public static bool IsSupportStandalonePlatform => Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer;

        public static object CreateBridgeImplementation(Type interfaceType, string startWith) {
            // 跳过初始化直接使用 TapLoom会在子线程被TapSDK.Core.BridgeCallback.Invoke 初始化
            TapLoom.Initialize();
            // 获取所有程序集
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();            
            // 查找以 startWith 开头的程序集
            var matchingAssemblies = allAssemblies
                .Where(assembly => assembly.GetName().FullName.StartsWith(startWith))
                .ToList();
            // 从匹配的程序集中查找实现指定接口的类
            List<Type> allCandidateTypes = new List<Type>();
            foreach (var assembly in matchingAssemblies) {
                try {
                    var types = assembly.GetTypes()
                        .Where(type => type.IsClass && interfaceType.IsAssignableFrom(type))
                        .ToList();
                    foreach (var type in types) {
                        allCandidateTypes.Add(type);
                    }
                }
                catch (Exception ex) {
                    TapLog.Error($"[TapTap] 获取程序集 {assembly.GetName().Name} 中的类型时出错: {ex.Message}");
                }
            }
            // 使用原始逻辑查找实现类
            Type bridgeImplementationType = null;
            try {
                bridgeImplementationType = matchingAssemblies
                    .SelectMany(assembly => {
                        try {
                            return assembly.GetTypes();
                        } catch {
                            return Type.EmptyTypes;
                        }
                    })
                    .SingleOrDefault(clazz => interfaceType.IsAssignableFrom(clazz) && clazz.IsClass);               
                // 如果使用 SingleOrDefault 没找到，尝试使用 FirstOrDefault
                if (bridgeImplementationType == null && allCandidateTypes.Count > 0) {
                    bridgeImplementationType = allCandidateTypes.FirstOrDefault();
                }
            }
            catch (Exception ex) {
                TapLog.Error($"[TapTap] 在查找实现类时发生异常: {ex.Message}\n{ex.StackTrace}");
            }
            if (bridgeImplementationType == null) {
                // 尝试在所有程序集中查找实现（不限制命名空间前缀）
                if (matchingAssemblies.Count == 0) {
                    List<Type> implementationsInAllAssemblies = new List<Type>();
                    foreach (var assembly in allAssemblies) {
                        try {
                            var types = assembly.GetTypes()
                                .Where(type => type.IsClass && !type.IsAbstract && interfaceType.IsAssignableFrom(type))
                                .ToList();
                            if (types.Count > 0) {
                                implementationsInAllAssemblies.AddRange(types);
                            }
                        }
                        catch { /* 忽略错误 */ }
                    }
                }
                return null;
            }
            try {
                return Activator.CreateInstance(bridgeImplementationType);
            }
            catch (Exception ex) {
                return null;
            }
        }
    }
}
