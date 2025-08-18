using TapSDK.Core;
using TapSDK.Core.Internal.Init;
using TapSDK.Achievement.Internal.Util;

namespace TapSDK.Achievement.Internal
{
    internal class TapAchievementInitTask : IInitTask
    {
        public int Order => 13;

        internal static bool IsInit = false;

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            TapTapAchievementOptions achievementOptions = null;
            if (otherOptions != null && otherOptions.Length > 0)
            {
                foreach (var option in otherOptions)
                {
                    if (option is TapTapAchievementOptions option1)
                    {
                        achievementOptions = option1;
                    }
                }
            }
            if (achievementOptions == null)
            {
                achievementOptions = new TapTapAchievementOptions();
            }
            TapTapAchievementManager.Instance.Init(coreOption.clientId, coreOption.region, achievementOptions);
            IsInit = true;
        }

        public void Init(TapTapSdkOptions coreOption)
        {
            TapTapAchievementManager.Instance.Init(coreOption.clientId, coreOption.region, new TapTapAchievementOptions());
            IsInit = true;
        }
    }
}
