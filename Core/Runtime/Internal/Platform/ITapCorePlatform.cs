using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TapSDK.Core.Internal
{
    public interface ITapCorePlatform
    {
        void Init(TapTapSdkOptions config);

        void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions);

        void UpdateLanguage(TapTapLanguageType language);

        Task<bool> IsLaunchedFromTapTapPC();

        void SendOpenLog(
            string project,
            string version,
            string action,
            Dictionary<string, string> properties
        );
    }
}
