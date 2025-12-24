using System;
using TapSDK.Core;
using TapSDK.Compliance.Model;
using TapSDK.Compliance.Internal;
using System.Threading.Tasks;

namespace TapSDK.Compliance
{
 
    public static class TapTapCompliance
    {

        public static readonly string Version = "4.10.0-beta.1";

        public static void RegisterComplianceCallback(Action<int, string> callback)
        {
            if (ComplianceJobManager.IsInit() == false) {
                TapLogger.Warn("TapSDK::ComplianceUIKit is not init, please call Init first!");
            }
            ComplianceJobManager.Job?.RegisterComplianceCallback(callback);
        }
        
        public static void Startup(string userId)
        {
            ComplianceJobManager.Job?.Startup(userId);
        }


        public static void Exit()
        {
            ComplianceJobManager.Job?.Exit();
        }

        /// <summary>
        /// 年龄类型:UNREALNAME = -1;CHILD = 0;TEEN = 8;YOUNG = 16; ADULT = 18;
        /// 当游戏旧版本不使用年龄段，新版本使用年龄段，对于老用户仍返回 -1
        /// </summary>
        public static Task<int> GetAgeRange()
        {
           
            IComplianceJob Job = ComplianceJobManager.Job;
            if (Job != null)
                return Job.GetAgeRange();

            var tcs = new TaskCompletionSource<int>();
            tcs.TrySetException(new Exception("TapCompliance Init failed"));
            return tcs.Task;    
        }

       

        /// <summary>
        /// 剩余时间(单位:秒)
        /// </summary>
        public static Task<int> GetRemainingTime()
        {
           
            IComplianceJob Job = ComplianceJobManager.Job;
            if (Job != null)
                return Job.GetRemainingTime();

            var tcs = new TaskCompletionSource<int>();
            tcs.TrySetException(new Exception("TapCompliance Init failed"));
            return tcs.Task;    
            
        }

        public static Task<string> GetCurrentAccessToken()
        {
            
            IComplianceJob Job = ComplianceJobManager.Job;
            if (Job != null)
                return Job.GetCurrentToken();

            var tcs = new TaskCompletionSource<string>();
            tcs.TrySetException(new Exception("TapCompliance Init failed"));
            return tcs.Task;    
        }
        
        /// <summary>
        /// 在支付前,检查支付结果
        /// </summary>
        /// <param name="amount">支付金额,单位:分</param>
        /// <param name="handleCheckPayLimit">检查支付结果的回调</param>
        /// <param name="handleCheckPayLimitException">检查支付碰到问题时的回调</param>
        public static void CheckPaymentLimit(long amount
            , Action<CheckPayResult> handleCheckPayLimit
            , Action<string> handleCheckPayLimitException)
        {
            ComplianceJobManager.Job?.CheckPaymentLimit(amount, handleCheckPayLimit, handleCheckPayLimitException);
        }
        
        /// <summary>
        /// 提交支付结果
        /// </summary>
        /// <param name="amount">支付金额,单位:分</param>
        /// <param name="handleSubmitPayResult">提交成功后的回调</param>
        /// <param name="handleSubmitPayResultException">提交失败后的回调</param>
        public static void SubmitPayment(long amount
            , Action handleSubmitPayResult
            , Action<string> handleSubmitPayResultException
        )
        {
            ComplianceJobManager.Job?.SubmitPayment(amount, handleSubmitPayResult, handleSubmitPayResultException);
        }
        

        /// <summary>
        /// 设置测试环境，需要在 startup 接口调用前设置
        /// </summary>
        /// <param name="enable">测试环境是否可用</param>
        [Obsolete("该方法已失效，不需要再额外调用")]
        public static void SetTestEnvironment(bool enable) {
                ComplianceJobManager.Job?.SetTestEnvironment(enable);
        }

        public static void OnInvokeExternalCallback(int code, string msg){
            ComplianceJobManager.Job?.OnInvokeExternalCallback(code,msg);
        }
        
    }
}