using UnityEngine;

namespace TapSDK.Core.Internal.Log
{
    public class TapLog
    {
        private const string TAG = "TapSDK";
        // 颜色常量
        private const string InfoColor = "#FFFFFF"; // 白色
        private const string WarningColor = "#FFFF00"; // 黄色
        private const string ErrorColor = "#FF0000"; // 红色
        private const string MainThreadColor = "#00FF00"; // 绿色
        private const string IOThreadColor = "#FF00FF"; // 紫色
        private const string TagColor = "#00FFFF"; // 青色

        // 开关变量，控制是否启用日志输出
        public static bool Enabled = false;

        private string module;
        private string tag;

        public TapLog(string module, string tag = TAG)
        {
            this.tag = tag;
            this.module = module;
        }

        public void Log(string message, string detail = null)
        {
            TapLog.Log(message, detail, tag, module);
        }

        // 输出带有自定义颜色和标签的警告
        public void Warning(string message, string detail = null)
        {
            TapLog.Warning(message, detail, tag, module);
        }

        // 输出带有自定义颜色和标签的错误
        public void Error(string message, string detail = null)
        {
            TapLog.Error(message, detail, tag, module);
        }

        // 输出带有自定义颜色和标签的普通日志
        public static void Log(string message, string detail = null, string tag = TAG, string module = null)
        {
            if (Enabled)
            {
                Debug.Log(GetFormattedMessage(message: message, detail: detail, colorHex: InfoColor, tag: tag, module: module));
            }
        }

        // 输出带有自定义颜色和标签的警告
        public static void Warning(string message, string detail = null, string tag = TAG, string module = null)
        {
            if (Enabled)
            {
                Debug.LogWarning(GetFormattedMessage(message: message, detail: detail, colorHex: WarningColor, tag: tag, module: module));
            }
        }

        // 输出带有自定义颜色和标签的错误
        public static void Error(string message, string detail = null, string tag = TAG, string module = null)
        {
            Debug.LogError(GetFormattedMessage(message: message, detail: detail, colorHex: ErrorColor, tag: tag, module: module));
        }

        // 格式化带有颜色和标签的消息
        private static string GetFormattedMessage(string message, string detail, string colorHex, string tag, string module)
        {
            string threadInfo = GetThreadInfo();
            string tagColor = TagColor;
            if (module != null && module != "")
            {
                tag = $"{tag}.{module}";
            }
            if (IsMobilePlatform())
            {
                return $"[{tag}] {threadInfo} {message}\n{detail}";
            }
            else
            {
                return $"<color={tagColor}>[{tag}]</color> {threadInfo} <color={colorHex}>{message}</color>\n{detail}\n";
            }

        }

        // 获取当前线程信息
        private static string GetThreadInfo()
        {
            bool isMainThread = System.Threading.Thread.CurrentThread.IsAlive && System.Threading.Thread.CurrentThread.ManagedThreadId == 1;
            string threadInfo = isMainThread ? "Main" : $"IO {System.Threading.Thread.CurrentThread.ManagedThreadId}";

            if (IsMobilePlatform())
            {
                // 移动平台的线程信息不使用颜色
                return $"({threadInfo})";
            }
            else
            {
                // 其他平台的线程信息使用颜色
                string color = isMainThread ? MainThreadColor : IOThreadColor;
                return $"<color={color}>({threadInfo})</color>";
            }
        }

        // 检查是否是移动平台
        private static bool IsMobilePlatform()
        {
            return Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
        }
    }
}
