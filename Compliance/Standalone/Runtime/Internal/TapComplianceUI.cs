using System;
using System.Threading.Tasks;
using TapSDK.Compliance.Model;
using TapSDK.Compliance.Internal;
using TapSDK.Core.Internal.Log;
using TapSDK.UI;

namespace TapSDK.Compliance
{
    public static class TapComplianceUI
    {
        // 主线程闸门统一收在 ComplianceMainThread.Run，说明见那边的注释。
        private static void RunOnUiThread(Action action) => ComplianceMainThread.Run(action);

        /// <summary>
        /// 这些面板里有一部分的调用方会 await 一个只能由面板回调（onOk / onSwitchAccount）
        /// 完成的 TaskCompletionSource。面板一旦没打开，那些回调永远不会触发——所以"打不开"
        /// 这件事必须能回传出去，否则合规的 Startup 会<b>永久挂起</b>，比抛异常难查得多。
        /// onFailed 为 null 时退化成 fire-and-forget（异常由闸门记日志），用于确实没有等待方
        /// 的调用点。
        /// </summary>
        private static Exception PanelMissing(string path)
        {
            return new Exception("打开合规面板失败，prefab 缺失: " + path);
        }

        /// <summary>
        /// 打开健康提醒窗口
        /// </summary>
        /// <param name="onFailed">
        /// 面板打不开时的回调。OnCheckedPlayableWithMinorAsync 在 RemainTime &gt; 0 时，
        /// tcs 只由 onOk 完成，必须传它，否则未成年人的 Startup 会一直挂着。
        /// </param>
        internal static void OpenHealthReminderPanel(PlayableResult playable, Action onOk = null,
            Action onSwitchAccount = null, Action<Exception> onFailed = null)
        {
            ComplianceMainThread.Run(() =>
            {
                var path = ComplianceConst.GetPrefabPath(ComplianceConst.HEALTH_REMINDER_PANEL_NAME,
                    TapTapComplianceManager.IsUseMobileUI());
                var healthReminderPanel = UIManager.Instance.OpenUI<TaptapComplianceHealthReminderController>(path);
                if (healthReminderPanel == null)
                {
                    onFailed?.Invoke(PanelMissing(path));
                    return;
                }
                healthReminderPanel.Show(playable, onOk, onSwitchAccount);
            }, onFailed);
        }

        /// <summary>
        /// 打开健康充值提醒窗口
        /// </summary>
        /// <param name="payable"></param>
        internal static void OpenHealthPaymentPanel(PayableResult payable)
        {
            // 这个重载的调用方（OnCheckedUnpayable）是 void、没有等待方，保持 fire-and-forget
            ComplianceMainThread.Run(() =>
            {
                var path = ComplianceConst.GetPrefabPath(ComplianceConst.HEALTH_PAYMENT_PANEL_NAME,
                    TapTapComplianceManager.IsUseMobileUI());
                var healthPaymentPanel = UIManager.Instance.OpenUI<TaptapComplianceHealthPaymentController>(path);
                if (healthPaymentPanel == null)
                {
                    TapLog.Error("[Compliance] " + PanelMissing(path).Message);
                    return;
                }
                healthPaymentPanel.Show(payable);
            });
        }

        /// <summary>
        /// 打开健康充值提醒窗口.填入自定义的文本内容
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="buttonText"></param>
        public static void OpenHealthPaymentPanel(string title, string content, string buttonText,
            Action onOk = null)
        {
            // 保持这个 public 签名原样：本类不在 Internal 命名空间下，给它加参数会改变方法
            // 签名、让已编译的调用方抛 MissingMethodException。需要回传失败的内部调用走下面
            // 那个 internal 重载。
            OpenHealthPaymentPanel(title, content, buttonText, onOk, null);
        }

        /// <param name="onFailed">
        /// 面板打不开时的回调。ShowVerifingTip 会 await 一个只由 onOk 完成的 tcs，必须传它。
        /// </param>
        internal static void OpenHealthPaymentPanel(string title, string content, string buttonText,
            Action onOk, Action<Exception> onFailed)
        {
            ComplianceMainThread.Run(() =>
            {
                var path = ComplianceConst.GetPrefabPath(ComplianceConst.HEALTH_PAYMENT_PANEL_NAME,
                    TapTapComplianceManager.IsUseMobileUI());
                var healthPaymentPanel = UIManager.Instance.OpenUI<TaptapComplianceHealthPaymentController>(path);
                if (healthPaymentPanel == null)
                {
                    onFailed?.Invoke(PanelMissing(path));
                    return;
                }
                healthPaymentPanel.Show(title, content, buttonText, onOk);
            }, onFailed);
        }

        /// <summary>
        /// 打开重试对话框
        /// </summary>
        internal static void ShowRetryDialog(string message, Action onRetry, string confirmButtonText = null)
        {
            ShowRetryDialog(message, onRetry, confirmButtonText, null);
        }

        /// <summary>
        /// onFailed 让返回 Task 的重载能把"面板压根没打开"这件事回传给等待方；为 null 时
        /// 退化成 fire-and-forget（异常由闸门记日志）。
        /// </summary>
        private static void ShowRetryDialog(string message, Action onRetry, string confirmButtonText,
            Action<Exception> onFailed)
        {
            ComplianceMainThread.Run(() =>
            {
                var path = ComplianceConst.GetPrefabPath(ComplianceConst.RETRY_ALERT_PANEL_NAME,
                    TapTapComplianceManager.IsUseMobileUI());
                var retryAlert =
                    UIManager.Instance.OpenUI<TaptapComplianceRetryAlertController>(path);
                if (retryAlert == null)
                {
                    // 面板没打开就没人会触发 onRetry，等 Task 的一方会一直挂着
                    onFailed?.Invoke(PanelMissing(path));
                    return;
                }
                retryAlert.Show(message, onRetry, confirmButtonText);
            }, onFailed);
        }

        public static Task ShowRetryDialog(string message, string confirmButtonText = null)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            // 失败必须回填：这个重载的调用方会 await 它，面板打不开时若不回填就永久挂起
            ShowRetryDialog(message, () => tcs.TrySetResult(null), confirmButtonText,
                e => tcs.TrySetException(e));
            return tcs.Task;
        }

    }
}
