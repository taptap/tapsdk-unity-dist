using TapSDK.Core.Internal.Utils;
using TapSDK.Core;

namespace TapSDK.Achievement.Internal
{
    public class TapTapAchievementManager
    {

        private static TapTapAchievementManager instance;
        private ITapTapAchievement platformWrapper;

        private TapTapAchievementManager()
        {
            platformWrapper = BridgeUtils.CreateBridgeImplementation(typeof(ITapTapAchievement), "TapSDK.Achievement") as ITapTapAchievement;
        }

        public static TapTapAchievementManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapTapAchievementManager();
                }
                return instance;
            }
        }

        public void Init(string clientId, TapTapRegionType regionType, TapTapAchievementOptions achievementOptions)
        {
            platformWrapper.Init(clientId, regionType, achievementOptions);
        }

        public void Increment(string achievementId, int step)
        {
            platformWrapper.Increment(achievementId, step);
        }

        public void Unlock(string achievementId)
        {
            platformWrapper.Unlock(achievementId);
        }

        public void ShowAchievements()
        {
            platformWrapper.ShowAchievements();
        }

        public void SetToastEnable(bool enable)
        {
            platformWrapper.SetToastEnable(enable);
        }

        public void RegisterCallBack(ITapAchievementCallback callback)
        {
            platformWrapper.RegisterCallBack(callback);
        }

        public void UnRegisterCallBack(ITapAchievementCallback callback)
        {
            platformWrapper.UnRegisterCallBack(callback);
        }
    }
}