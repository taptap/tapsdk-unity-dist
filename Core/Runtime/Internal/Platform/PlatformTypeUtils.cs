using System;
using System.Linq;
using UnityEngine;

namespace TapSDK.Core.Internal {
    public static class PlatformTypeUtils {
        /// <summary>
        /// 创建平台接口实现类对象
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="startWith"></param>
        /// <returns></returns>
        public static object CreatePlatformImplementationObject(Type interfaceType, string startWith) {
            // Debug.Log($"Searching for types in assemblies starting with: {startWith} that implement: {interfaceType}");

            // 获取所有符合条件的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().FullName.StartsWith(startWith));

            // foreach (var assembly in assemblies) {
                // Debug.Log($"Found assembly: {assembly.GetName().FullName}");
            // }

            // 获取符合条件的类型
            Type platformSupportType = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .SingleOrDefault(clazz => interfaceType.IsAssignableFrom(clazz) && clazz.IsClass);

            if (platformSupportType != null) {
                // Debug.Log($"Found type: {platformSupportType.FullName}, creating instance...");
                try
                {
                    return Activator.CreateInstance(platformSupportType);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create instance of {platformSupportType.FullName}: {ex}");
                }
            } else {
                Debug.LogError($"No type found that implements {interfaceType} in assemblies starting with {startWith}");
            }

            return null;
        }
    }
}