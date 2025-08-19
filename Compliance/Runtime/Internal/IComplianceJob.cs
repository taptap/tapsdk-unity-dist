using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TapSDK.Core;

namespace TapSDK.Compliance.Model
{
    public interface IComplianceJob
    {
        /// <summary>
        /// 根据状态的对外回调
        /// </summary>
        List<Action<int, string>> ExternalCallbackList { get;}
        
        Task<int> GetAgeRange();
        /// <summary>
        /// 剩余时间(单位:秒)
        /// </summary>
        Task<int> GetRemainingTime();
        
        Task<string> GetCurrentToken() ;
        
        /// <summary>
        /// 新的初始化接口
        /// </summary>
        /// <param name="config"></param>
        void Init(string clientId, string clientToken, TapTapRegionType regionType, TapTapComplianceOption config);
        /// <summary>
        /// 设置防沉迷回调
        /// </summary>
        /// <param name="callback">int 代表返回 code, string 代表 message</param>
        void RegisterComplianceCallback(Action<int, string> callback);
        

        void Startup(string userId);

        void Exit();
        
        void CheckPaymentLimit(long amount
            , Action<CheckPayResult> handleCheckPayLimit
            , Action<string> handleCheckPayLimitException);

        void SubmitPayment(long amount
            , Action handleSubmitPayResult
            , Action<string> handleSubmitPayResultException);
        

        void SetTestEnvironment(bool enable);

        void OnInvokeExternalCallback(int code, string msg);
    }
}
