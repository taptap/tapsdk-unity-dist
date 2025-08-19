using System;
using System.Threading.Tasks;
using TapSDK.Compliance.Model;
using System.Collections.Generic;
using TapSDK.Core;
using TapSDK.Compliance.Mobile.Runtime;

namespace TapSDK.Compliance.Mobile.Runtime
{
    public sealed class ComplianceMobileOldJob : IComplianceJob
    {
        private List<Action<int, string>> _externalCallbackList;

        public List<Action<int, string>> ExternalCallbackList
        {
            get => _externalCallbackList;
        }


        /// <summary>
        /// 剩余时间(单位:秒)
        /// </summary>
        public Task<int> GetRemainingTime()
        {
            return ComplianceMobileBridge.GetCurrentUserRemainTime();
        }

        public Task<string> GetCurrentToken()
        {
            return ComplianceMobileBridge.GetCurrentAccessToken();
        }

        public Task<int> GetAgeRange()
        {
            return ComplianceMobileBridge.GetUserAgeRange();
        }

        public void Init(string clientId, string clientToken, TapTapRegionType regionType, TapTapComplianceOption config)
        {

        }

        public void RegisterComplianceCallback(Action<int, string> callback)
        {
            ComplianceMobileBridge.RegisterComplianceCallback(callback);
        }

        public void Startup(string userId)
        {
            ComplianceMobileBridge.Startup(userId);
        }


        public void Exit()
        {
            ComplianceMobileBridge.Exit();
        }


        public void CheckPaymentLimit(long amount, Action<CheckPayResult> handleCheckPayLimit, Action<string> handleCheckPayLimitException)
        {
            ComplianceMobileBridge.CheckPaymentLimit(amount, handleCheckPayLimit, handleCheckPayLimitException);
        }

        public void SubmitPayment(long amount, Action handleSubmitPayResult, Action<string> handleSubmitPayResultException)
        {
            ComplianceMobileBridge.SubmitPayResult(amount, handleSubmitPayResult, handleSubmitPayResultException);
        }


        public void SetTestEnvironment(bool enable)
        {
            ComplianceMobileBridge.SetTestEnvironment(enable);
        }

        public void OnInvokeExternalCallback(int code, string msg)
        {

        }

    }
}
