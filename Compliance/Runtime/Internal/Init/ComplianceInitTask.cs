using TapSDK.Compliance.Model;
using TapSDK.Core;
using TapSDK.Core.Internal.Init;

namespace TapSDK.Compliance.Internal.Init
{
    public class ComplianceInitTask : IInitTask
    {
        public int Order => 12;

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            if (coreOption == null)
            {
                return;
            }
            TapTapComplianceOption complianceOption = null;
            if (otherOptions != null && otherOptions.Length > 0)
            {
                foreach (var option in otherOptions)
                {
                    if (option is TapTapComplianceOption option1)
                    {
                        complianceOption = option1;
                    }
                }
            }
            if (complianceOption == null)
            {
                complianceOption = new TapTapComplianceOption();
            }
            ComplianceJobManager.Init(coreOption.clientId, coreOption.clientToken, coreOption.region, complianceOption);
        }

        public void Init(TapTapSdkOptions coreOption)
        {
            if (coreOption == null)
            {
                return;
            }
            TapTapComplianceOption option = new TapTapComplianceOption();
            ComplianceJobManager.Init(coreOption.clientId, coreOption.clientToken, coreOption.region, option);

        }
    }
}