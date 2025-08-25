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
            TapLog.Log($"[TapTap] 开始查找实现类: interfaceType={interfaceType.FullName}, startWith={startWith}, 当前平台={Application.platform}");
            
            // 跳过初始化直接使用 TapLoom会在子线程被TapSDK.Core.BridgeCallback.Invoke 初始化
            TapLoom.Initialize();
            
            // 获取所有程序集
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            TapLog.Log($"[TapTap] 已加载的程序集总数: {allAssemblies.Length}");
            
            // 查找以 startWith 开头的程序集
            var matchingAssemblies = allAssemblies
                .Where(assembly => assembly.GetName().FullName.StartsWith(startWith))
                .ToList();
            
            TapLog.Log($"[TapTap] 找到匹配 '{startWith}' 的程序集数量: {matchingAssemblies.Count}");
            
            // 打印匹配的程序集名称
            foreach (var assembly in matchingAssemblies) {
                TapLog.Log($"[TapTap] 匹配的程序集: {assembly.GetName().FullName}");
            }
            
            // 如果没有找到匹配的程序集，打印所有TapSDK相关的程序集
            if (matchingAssemblies.Count == 0) {
                var tapAssemblies = allAssemblies
                    .Where(assembly => assembly.GetName().FullName.Contains("TapSDK") || 
                                      assembly.GetName().FullName.Contains("TapTap"))
                    .ToList();
                
                TapLog.Log($"[TapTap] 未找到匹配的程序集，但找到 {tapAssemblies.Count} 个相关程序集:");
                foreach (var assembly in tapAssemblies) {
                    TapLog.Log($"[TapTap]  - {assembly.GetName().FullName}");
                }
            }
            
            // 从匹配的程序集中查找实现指定接口的类
            List<Type> allCandidateTypes = new List<Type>();
            foreach (var assembly in matchingAssemblies) {
                try {
                    var types = assembly.GetTypes()
                        .Where(type => type.IsClass && interfaceType.IsAssignableFrom(type))
                        .ToList();
                    
                    TapLog.Log($"[TapTap] 在程序集 {assembly.GetName().Name} 中找到 {types.Count} 个实现 {interfaceType.Name} 的类");
                    
                    foreach (var type in types) {
                        TapLog.Log($"[TapTap]  - {type.FullName} (IsPublic: {type.IsPublic}, IsAbstract: {type.IsAbstract})");
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
                
                TapLog.Log($"[TapTap] SingleOrDefault 查找结果: {(bridgeImplementationType != null ? bridgeImplementationType.FullName : "null")}");
                
                // 如果使用 SingleOrDefault 没找到，尝试使用 FirstOrDefault
                if (bridgeImplementationType == null && allCandidateTypes.Count > 0) {
                    TapLog.Log($"[TapTap] SingleOrDefault 未找到实现，但有 {allCandidateTypes.Count} 个候选类型，尝试使用 FirstOrDefault");
                    bridgeImplementationType = allCandidateTypes.FirstOrDefault();
                    TapLog.Log($"[TapTap] FirstOrDefault 查找结果: {(bridgeImplementationType != null ? bridgeImplementationType.FullName : "null")}");
                }
                
                // 如果找到多个实现，可能是 SingleOrDefault 失败的原因
                if (allCandidateTypes.Count > 1) {
                    TapLog.Warning($"[TapTap] 找到多个实现 {interfaceType.Name} 的类，这可能导致 SingleOrDefault 返回 null");
                    foreach (var type in allCandidateTypes) {
                        TapLog.Warning($"[TapTap]  - {type.FullName}");
                    }
                }
            }
            catch (Exception ex) {
                TapLog.Error($"[TapTap] 在查找实现类时发生异常: {ex.Message}\n{ex.StackTrace}");
            }
            
            if (bridgeImplementationType == null) {
                TapLog.Warning($"[TapTap] TapSDK 无法为 {interfaceType} 找到平台 {Application.platform} 上的实现类。");
                
                // 尝试在所有程序集中查找实现（不限制命名空间前缀）
                if (matchingAssemblies.Count == 0) {
                    TapLog.Log("[TapTap] 尝试在所有程序集中查找实现...");
                    List<Type> implementationsInAllAssemblies = new List<Type>();
                    
                    foreach (var assembly in allAssemblies) {
                        try {
                            var types = assembly.GetTypes()
                                .Where(type => type.IsClass && !type.IsAbstract && interfaceType.IsAssignableFrom(type))
                                .ToList();
                            
                            if (types.Count > 0) {
                                TapLog.Log($"[TapTap] 在程序集 {assembly.GetName().Name} 中找到 {types.Count} 个实现");
                                implementationsInAllAssemblies.AddRange(types);
                            }
                        }
                        catch { /* 忽略错误 */ }
                    }
                    
                    if (implementationsInAllAssemblies.Count > 0) {
                        TapLog.Log($"[TapTap] 在所有程序集中找到 {implementationsInAllAssemblies.Count} 个实现:");
                        foreach (var type in implementationsInAllAssemblies) {
                            TapLog.Log($"[TapTap]  - {type.FullName} (在程序集 {type.Assembly.GetName().Name} 中)");
                        }
                    }
                }
                
                return null;
            }
            
            try {
                TapLog.Log($"[TapTap] 创建 {bridgeImplementationType.FullName} 的实例");
                return Activator.CreateInstance(bridgeImplementationType);
            }
            catch (Exception ex) {
                TapLog.Error($"[TapTap] 创建实例时出错: {ex.Message}");
                return null;
            }
        }
    }
}
