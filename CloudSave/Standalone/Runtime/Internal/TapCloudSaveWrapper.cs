using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TapSDK.CloudSave.Standalone
{
    internal class TapCloudSaveWrapper
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        internal const string DllName = "cloudsave_sdk";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        internal const string DllName = "libcloudsave_sdk";
#endif

        /**
        * 初始化接口，只需要调用一次。非线程安全，并发调用可能崩溃。
        *
        *   cfg 初始化配置，JSON 格式：
        *
        *   TapSDK参数格式
        *   {
        *       "region": 2,
        *       "log_to_console": 1,
        *       "log_level": 3,
        *       "data_dir": "/tmp",
        *       "client_id": "***",
        *       "client_token": "***",
        *       "ua": "TapSDK-Android/3.28.0",
        *       "lang": "zh-CN",
        *       "platform": "Android",
        *       "device_id": "123456",
        *       "sdk_artifact": "Android",
        *       "sdk_module_ver": "4.6.0",
        *       "sdk_token": {
        *           "kid": "***",
        *           "key": "***"
        *       }
        *   }
        *
        *
        *   - region 取值：0 国内、1 海外、2 RND、3 海外RND
        *   - log_to_console 是否输出到控制台：0 不输出、1 输出。
        *   - log_level 取值：1 Trace、2 Debug、3 Info、4 Warn、5 Error、6 完全不输出
        *   - data_dir 保存本地缓存和日志文件的目录，不允许为空
        *   - client_id 不允许为空
        *   - client_token 不允许为空
        *   - ua user agent，不允许为空
        *   - lang 语言，允许为空
        *   - platform 不允许为空，TapSDK专用参数
        *   - device_id 设备ID，不允许为空，TapSDK专用参数
        *   - sdk_artifact 不允许为空，TapSDK专用参数
        *   - sdk_module_ver 不允许为空，TapSDK专用参数
        *   - sdk_token 登录态鉴权token，允许为空，TapSDK专用参数
        *     - kid mac_key id，不允许为空
        *     - key mac密钥，不允许为空
        *   - runtime_ver 宿主版本，不允许为空，Tap Miniapp专用参数
        *   - access_token 登录态鉴权token，允许为空，Tap Miniapp专用参数
        *     - kid mac_key id，不允许为空
        *     - key mac密钥，不允许为空
        *
        * 成功返回 0，失败返回 -1
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkCloudSaveInitialize(string cfg);

        /**
         * 用于释放TapSdkCloudSaveCreateArchive、TapSdkCloudSaveGetArchiveList、TapSdkCloudSaveGetArchiveData等函数返回的堆内存
         *
         * @param data 需要释放的内存
         */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void TapSdkCloudSaveFreeMemory(IntPtr intPtr);

        /**
        * access token发生变化（比如切换账号）时调用。收到接口报access_denied错误时，SDK使用方需要重新登录，然后调用该接口更新access token
        *
        * @param token 最新的access token，传空指针或"{}"表示用户退出登录。格式如下：
        *    {
        *       "kid": "***",
        *       "key": "***"
        *    }
        *
        * @return 成功返回 0，失败（通常是JSON格式错误）返回 -1
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkCloudSaveUpdateAccessToken(string token);

        /**
        * 创建云存档，同步等待服务端返回，然后更新本地存档缓存。不允许并发调用，否则直接抛错
        *
        * @param metadata 存档元信息，JSON格式
        *     {
        *       "name": "存档名，60字节以内，不允许空，不允许汉字",
        *       "summary": "存档描述，500字节以内，不允许空",
        *       "extra": "用户自定义信息，1000字节以内，允许空",
        *       "playtime": 0
        *     }
        *     - playtime 秒级
        * @param archiveData 存档二进制数据，不允许空指针
        * @param archiveDataSize saveData的大小（字节），不允许为0
        * @param coverData 封面二进制数据，允许空指针
        * @param coverDataSize saveData的大小（字节），允许为0
        * @param extraParams Tap Miniapp专用参数，JSON格式。其他接入方传空指针即可
        *     {
        *       "X-UA": "***",
        *       "miniappId": "***"
        *     }
        *
        * @return JSON格式，服务端生成的云存档UUID和FileID，或者错误信息。用完后，调用方需要调用TapSdkCloudSaveFreeMemory来释放内存
        *     {
        *       "data": {
        *           "uuid": "存档UUID",
        *           "file_id": "存档文件ID，用于下载文件"
        *       },
        *       "now": 1748340775,
        *       "success": true
        *     }
        * @note 对于Tap Miniapp，JSON字段使用驼峰风格。比如上面的返回示例，对于Tap Miniapp，是"fileId"而不是"file_id"。
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr TapSdkCloudSaveCreateArchive(
            string metadata,
            byte[] archiveData,
            int archiveDataSize,
            byte[] coverData,
            int coverDataSize,
            string extraParams
        );

        /**
        * 更新云存档，同步等待服务端返回，然后更新本地存档缓存。不允许并发调用，否则直接抛错
        *
        * @param archiveUUID TapSdkCloudSaveCreateArchive()返回的云存档UUID，比如："1bffaa173dde2e7afb5adf6442971961"。必须先创建云存档成功后，通过云存档UUID来更新
        * @param metadata 存档元信息，JSON格式
        *     {
        *       "name": "存档名，60字节以内，不允许空，不允许汉字",
        *       "summary": "存档描述，500字节以内，不允许空",
        *       "extra": "用户自定义信息，1000字节以内，允许空",
        *       "playtime": 0
        *     }
        *     - playtime 秒级
        * @param archiveData 存档二进制数据，不允许空指针
        * @param archiveDataSize saveData的大小（字节），不允许为0
        * @param coverData 封面二进制数据，允许空指针
        * @param coverDataSize saveData的大小（字节），允许为0
        * @param extraParams Tap Miniapp专用参数，JSON格式。其他接入方传空指针即可
        *     {
        *       "X-UA": "***",
        *       "miniappId": "***"
        *     }
        *
        * @return JSON格式，服务端生成的云存档UUID和FileID，或者错误信息。用完后，调用方需要调用TapSdkCloudSaveFreeMemory来释放内存
        *     {
        *       "data": {
        *           "uuid": "存档UUID",
        *           "file_id": "存档文件ID，用于下载文件"
        *       },
        *       "now": 1748340775,
        *       "success": true
        *     }
        * @note 对于Tap Miniapp，JSON字段使用驼峰风格。比如上面的返回示例，对于Tap Miniapp，是"fileId"而不是"file_id"。
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr TapSdkCloudSaveUpdateArchive(
            string archiveUUID,
            string metadata,
            byte[] archiveData,
            int archiveDataSize,
            byte[] coverData,
            int coverDataSize,
            string extraParams
        );

        /**
        * 发起删除存档请求，同步等待服务端返回，然后删除本地存档缓存。允许并发调用
        *
        * @param archiveUUID TapSdkCloudSaveCreateArchive()/TapSdkCloudSaveGetArchiveList()返回的云存档UUID，比如："1bffaa173dde2e7afb5adf6442971961"。必须先创建云存档成功后，通过云存档UUID来删除
        * @param extraParams Tap Miniapp专用参数，JSON格式。其他接入方传空指针即可
        *     {
        *       "X-UA": "***",
        *       "miniappId": "***"
        *     }
        *
        * @return JSON格式，返回成功或者错误信息。用完后，调用方需要调用TapSdkCloudSaveFreeMemory来释放内存
        *     {
        *         "data": {
        *            "uuid": "被删掉的云存档UUID"
        *         },
        *         "now": 1748487810,
        *         "success": true
        *     }
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr TapSdkCloudSaveDeleteArchive(
            string archiveUUID,
            string extraParams
        );

        /**
         * 获取存档元信息列表，同步等待服务端返回。允许并发调用
         *
         * @param extraParams Tap Miniapp专用参数，JSON格式。其他接入方传空指针即可
         *     {
         *       "X-UA": "***",
         *       "miniappId": "***"
         *     }
         *
         * @return JSON格式，返回存档元信息列表，或者错误信息。用完后，需要调用TapSdkCloudSaveFreeMemory来释放内存
         *     {
         *       "data": {
         *         "saves": [
         *           {
         *             "uuid": "738b6c08bdbe459b96607dd10f83d177",
         *             "file_id": "738b6c08bdbe459b96607dd10f83d177",
         *             "name": "save",
         *             "save_size": 184237,
         *             "cover_size": 7138,
         *             "summary": "i love this game",
         *             "extra": "what the hell",
         *             "playtime": 0,
         *             "created_time": 1748587677,
         *             "modified_time": 1748587677
         *           }
         *         ]
         *       },
         *       "now": 1748588091,
         *       "success": true
         *     }
         *     - playtime 秒级
         * @note 对于Tap Miniapp，JSON字段使用驼峰风格。比如上面的返回示例，对于Tap Miniapp，是"fileId/saveSize"而不是"file_id/save_size"。
         */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr TapSdkCloudSaveGetArchiveList(string extraParams);

        /**
        * 读取存档文件，优先使用本地缓存，不存在才下载
        *
        * @param archiveUUID TapSdkCloudSaveCreateArchive()/TapSdkCloudSaveGetArchiveList()返回的云存档UUID，必须先创建云存档成功后，才能读取
        * @param archiveFileID TapSdkCloudSaveCreateArchive()/TapSdkCloudSaveGetArchiveList()返回的存档文件FileID，必须先创建云存档成功后，才能读取
        * @param archiveDataSize 出参，实际读取到的存档文件大小。如果读取失败，则其值会是负数
        * @param extraParams Tap Miniapp专用参数，JSON格式。其他接入方传空指针即可
        *     {
        *       "X-UA": "***",
        *       "miniappId": "***"
        *     }
        *
        * @return 成功时saveSize是实际读取到的存档文件大小，返回存档文件内容；失败时saveSize是负数，返回JSON格式的错误信息，格式如下。用完后，需要调用TapSdkCloudSaveFreeMemory来释放内存
        *    {
        *       "data": {
        *         "code": -1,
        *         "msg": "InvalidArgument: BINDING: Key: 'GetSavesRequest.uuid' Error:Field validation for 'uuid' failed on the 'len' tag",
        *         "error": "invalid_request",
        *         "error_description": "InvalidArgument: BINDING: Key: 'GetSavesRequest.uuid' Error:Field validation for 'uuid' failed on the 'len' tag"
        *       },
        *       "now": 1748915185,
        *       "success": false
        *     }
        * @note 对于Tap Miniapp，JSON字段使用驼峰风格。比如上面的返回示例，对于Tap Miniapp，是"errorDescription"而不是"error_description"。
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr TapSdkCloudSaveGetArchiveData(
            string archiveUUID,
            string archiveFileID,
            out int archiveDataSize,
            string extraParams
        );

        /**
         * 读取封面文件，优先使用本地缓存，不存在才下载
         *
         * @param archiveUUID TapSdkCloudSaveCreateArchive()/TapSdkCloudSaveGetArchiveList()返回的云存档UUID，必须先创建云存档成功后，才能读取
         * @param archiveFileID TapSdkCloudSaveCreateArchive()/TapSdkCloudSaveGetArchiveList()返回的存档文件FileID，必须先创建云存档成功后，才能读取
         * @param coverSize 出参，实际读取到的封面文件大小。如果读取失败，则其值会是负数
         * @param extraParams Tap Miniapp专用参数，JSON格式。其他接入方传空指针即可
         *     {
         *       "X-UA": "***",
         *       "miniappId": "***"
         *     }
         *
         * @return 成功时coverSize是实际读取到的封面文件大小，返回cover文件内容；失败时coverSize是负数，返回JSON格式的错误信息，格式如下。用完后，需要调用TapSdkCloudSaveFreeMemory来释放内存
         *    {
         *       "data": {
         *         "code": -1,
         *         "msg": "InvalidArgument: BINDING: Key: 'GetSavesRequest.uuid' Error:Field validation for 'uuid' failed on the 'len' tag",
         *         "error": "invalid_request",
         *         "error_description": "InvalidArgument: BINDING: Key: 'GetSavesRequest.uuid' Error:Field validation for 'uuid' failed on the 'len' tag"
         *       },
         *       "now": 1748915185,
         *       "success": false
         *     }
         * @note 对于Tap Miniapp，JSON字段使用驼峰风格。比如上面的返回示例，对于Tap Miniapp，是"errorDescription"而不是"error_description"。
         */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr TapSdkCloudSaveGetArchiveCover(
            string archiveUUID,
            string archiveFileID,
            out int coverSize,
            string extraParams
        );

        /**
         * 设置日志等级
         *   - logLevel 日志等级：1 trace、2 debug、3 info、4 warn、5 error、> 5 不打日志。建议调试时设为1，正式版设为3。
         *   - logToConsole 是否输出到控制台：0 不输出、1 输出。
         */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void TapSdkCloudSaveSetLogLevel(int logLevel, int logToConsole);

        /**
         * 代码版本，如：1.2.5
         *
         * @return SDK版本号，全局静态变量，无需释放内存
         */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr TapSdkCloudSaveVersion();

        /**
         * git commit 版本，如：98f5d81a0fdcab9a755878b3e825c2cb510e5196
         *
         * @return git commit版本，全局静态变量，无需释放内存
         */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr TapSdkCloudSaveGitCommit();

        internal static string CreateArchive(
            string metadata,
            byte[] archiveData,
            int archiveDataSize,
            byte[] coverData,
            int coverDataSize
        )
        {
            IntPtr intPtr = TapSdkCloudSaveCreateArchive(
                metadata,
                archiveData,
                archiveDataSize,
                coverData,
                coverDataSize,
                null
            );
            try
            {
                string result = GetUTF8String(intPtr);
                return result;
            }
            finally
            {
                TapSdkCloudSaveFreeMemory(intPtr);
            }
        }

        internal static string UpdateArchive(
            string archiveUUID,
            string metadata,
            byte[] archiveData,
            int archiveDataSize,
            byte[] coverData,
            int coverDataSize
        )
        {
            IntPtr intPtr = TapSdkCloudSaveUpdateArchive(
                archiveUUID,
                metadata,
                archiveData,
                archiveDataSize,
                coverData,
                coverDataSize,
                null
            );
            try
            {
                string result = GetUTF8String(intPtr);
                return result;
            }
            finally
            {
                TapSdkCloudSaveFreeMemory(intPtr);
            }
        }

        internal static string DeleteArchive(string archiveUUID)
        {
            IntPtr intPtr = TapSdkCloudSaveDeleteArchive(archiveUUID, null);
            try
            {
                string result = GetUTF8String(intPtr);
                return result;
            }
            finally
            {
                TapSdkCloudSaveFreeMemory(intPtr);
            }
        }

        internal static string GetArchiveList()
        {
            IntPtr intPtr = TapSdkCloudSaveGetArchiveList(null);
            try
            {
                string result = GetUTF8String(intPtr);
                return result;
            }
            finally
            {
                TapSdkCloudSaveFreeMemory(intPtr);
            }
        }

        internal static byte[] GetArchiveData(
            string archiveUUID,
            string archiveFileID,
            out int archiveDataSize
        )
        {
            int dataSize = 0;
            IntPtr intPtr = TapSdkCloudSaveGetArchiveData(
                archiveUUID,
                archiveFileID,
                out dataSize,
                null
            );
            try
            {
                byte[] result;
                if (dataSize >= 0)
                {
                    if (dataSize == 0)
                    {
                        result = new byte[0];
                    }
                    else
                    {
                        result = GetOriginByte(intPtr, dataSize);
                    }
                }
                else
                {
                    result = GetUTF8Byte(intPtr);
                }
                archiveDataSize = dataSize;
                return result;
            }
            finally
            {
                TapSdkCloudSaveFreeMemory(intPtr);
            }
        }

        internal static byte[] GetArchiveCover(
            string archiveUUID,
            string archiveFileID,
            out int coverSize
        )
        {
            int dataSize;
            byte[] result;
            IntPtr intPtr = TapSdkCloudSaveGetArchiveCover(
                archiveUUID,
                archiveFileID,
                out dataSize,
                null
            );
            try
            {
                coverSize = dataSize;
                if (dataSize >= 0)
                {
                    if (dataSize == 0)
                    {
                        result = new byte[0];
                    }
                    else
                    {
                        result = GetOriginByte(intPtr, dataSize);
                    }
                }
                else
                {
                    result = GetUTF8Byte(intPtr);
                }
                return result;
            }
            finally
            {
                TapSdkCloudSaveFreeMemory(intPtr);
            }
        }

        private static string GetUTF8String(IntPtr ptr)
        {
            byte[] buffer = GetUTF8Byte(ptr);
            // 3. 按 UTF-8 解码
            return Encoding.UTF8.GetString(buffer);
        }

        private static byte[] GetUTF8Byte(IntPtr ptr)
        {
            // 1. 获取字符串长度（假设以 null 结尾）
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
            {
                len++;
            }

            // 2. 复制字节到托管数组
            byte[] buffer = new byte[len];
            Marshal.Copy(ptr, buffer, 0, len);
            return buffer;
        }

        private static byte[] GetOriginByte(IntPtr ptr, int length)
        {
            byte[] copyByte = new byte[length];
            Marshal.Copy(ptr, copyByte, 0, length);
            return copyByte;
        }
    }
}
