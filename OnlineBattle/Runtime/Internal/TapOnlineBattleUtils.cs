using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using TapSDK.Login;

namespace TapSDK.OnlineBattle
{
    internal class TapOnlineBattleUtils
    {
        // 生成请求唯一 ID 基数
        private static long RequestIDCounter = 0;

        private static TapLog onlineBattleLog;

        // 辅助函数：IntPtr -> byte[]
        internal static string PtrToString(IntPtr data, uint dataLen)
        {
            if (data == IntPtr.Zero || dataLen == 0)
                return "";

            byte[] buffer = new byte[dataLen];
            Marshal.Copy(data, buffer, 0, (int)dataLen);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// 将字符串转为存储该数据的指针，使用完后务必调用 free 释放内存
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static (IntPtr ptr, uint len, Action free) StringToPtr(string str)
        {
            if (str == null)
                return (IntPtr.Zero, 0, () => { });

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length + 1); // +1 for '\0'

            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0);

            Action free = () =>
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                    ptr = IntPtr.Zero;
                }
            };

            return (ptr, (uint)bytes.Length, free);
        }

        /// <summary>
        /// 根据 Native 层返回的错误结构生成异常
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static TapException GenetateExceptionByData(Dictionary<string, object> data)
        {
            int errorCode = -1;
            string errorMsg = "unknown error";
            if (data != null)
            {
                if (data.TryGetValue("code", out object codeObj))
                {
                    errorCode = Convert.ToInt32(codeObj);
                }
                if (data.TryGetValue("msg", out object msgObj) && msgObj is string msgValue)
                {
                    errorMsg = msgValue;
                }
            }
            return new TapException(errorCode, errorMsg);
        }

        /// <summary>
        /// 生成唯一请求 ID
        /// </summary>
        /// <returns></returns>
        internal static long GenerateRequestID()
        {
            return Interlocked.Increment(ref RequestIDCounter);
        }

        internal static void RunOnMainThread(Action action)
        {
            TapLoom.QueueOnMainThread(action);
        }

        /// <summary>
        /// 获取当前 Tap 登录信息，由于 Android 桥接调用需确保在 unity 主线程
        /// </summary>
        /// <returns></returns>
        internal static Task<TapTapAccount> GetCurrentTapAccount()
        {
            TaskCompletionSource<TapTapAccount> taskCompletionSource =
                new TaskCompletionSource<TapTapAccount>();
            RunOnMainThread(async () =>
            {
                try
                {
                    TapTapAccount tapTapAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
                    taskCompletionSource.TrySetResult(tapTapAccount);
                }
                catch (Exception e)
                {
                    taskCompletionSource.TrySetResult(null);
                }
            });
            return taskCompletionSource.Task;
        }

        internal static string GetTrackMethodName(MethodName value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = (DescriptionAttribute)
                Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attr?.Description ?? value.ToString();
        }

        internal static void Log(string msg, bool isError = false)
        {
            if (onlineBattleLog == null)
            {
                onlineBattleLog = new TapLog("TapOnlineBattle");
            }
            if (!string.IsNullOrEmpty(msg))
            {
                if (isError)
                {
                    onlineBattleLog.Error(msg);
                }
                else
                {
                    onlineBattleLog.Log(msg);
                }
            }
        }
    }
}
