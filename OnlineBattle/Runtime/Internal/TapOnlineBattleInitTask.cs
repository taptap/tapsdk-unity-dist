using TapSDK.Core;
using TapSDK.Core.Internal.Init;

#if !UNITY_EDITOR_OSX
namespace TapSDK.OnlineBattle.Internal.Init
{
    public sealed class TapOnlineBattleInitTask : IInitTask
    {
        public int Order => 18;

        public void Init(TapTapSdkOptions coreOption)
        {
            TapTapOnlineBattle.Init(coreOption);
        }

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            TapTapOnlineBattle.Init(coreOption);
        }
    }
}
#endif