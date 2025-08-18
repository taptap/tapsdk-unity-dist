using TapSDK.Login;
using TapSDK.Login.Standalone;

namespace TapSDK.Compliance
{
    public class TapLoginPermissionConfig : IComplianceProvider
    {
        public string GetAgeRangeScope(bool isCN)
        {
            if (TapTapComplianceManager.ClientId != null && isCN)
            {
                if (TapTapComplianceManager.ComplianceConfig.useAgeRange)
                {
                    return ComplianceWorker.SCOPE_COMPLIANCE;
                }
                else
                {
                    return ComplianceWorker.SCOPE_COMPLIANCE_BASIC;
                }
            }
            return null;
        }
    }
}
