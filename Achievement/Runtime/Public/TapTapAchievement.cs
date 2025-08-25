using TapSDK.Achievement.Internal;
using TapSDK.Achievement.Internal.Util;

namespace TapSDK.Achievement
{
    public class TapTapAchievement
    {
        public static readonly string Version = "4.8.1-beta.6";

        public static void Increment(string achievementId, int step)
        {
            TapAchievementLog.Log($"TapTapAchievement -- Increment achievementId: {achievementId}, step: {step}");
            TapTapAchievementManager.Instance.Increment(achievementId, step);
        }

        public static void Unlock(string achievementId)
        {
            TapAchievementLog.Log($"TapTapAchievement -- Unlock achievementId: {achievementId}");
            TapTapAchievementManager.Instance.Unlock(achievementId);
        }

        public static void ShowAchievements()
        {
            TapAchievementLog.Log("TapTapAchievement -- ShowAchievements");
            TapTapAchievementManager.Instance.ShowAchievements();
        }

        public static void SetToastEnable(bool enable)
        {
            TapAchievementLog.Log($"TapTapAchievement -- SetToastEnable = {enable}");
            TapTapAchievementManager.Instance.SetToastEnable(enable);
        }

        public static void RegisterCallBack(ITapAchievementCallback callback)
        {
            TapAchievementLog.Log("TapTapAchievement -- RegisterCallBack");
            TapTapAchievementManager.Instance.RegisterCallBack(callback);
        }

        public static void UnRegisterCallBack(ITapAchievementCallback callback)
        {
            TapAchievementLog.Log("TapTapAchievement -- UnRegisterCallBack");
            TapTapAchievementManager.Instance.UnRegisterCallBack(callback);
        }
    }
}