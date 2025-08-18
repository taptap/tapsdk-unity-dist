using System;
using System.Threading.Tasks;

namespace TapSDK.Core.Internal {
    public interface ITapCorePlatform {
        void Init(TapTapSdkOptions config);

        void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions);
        
        void UpdateLanguage(TapTapLanguageType language);

        Task<bool> IsLaunchedFromTapTapPC();
    }
}