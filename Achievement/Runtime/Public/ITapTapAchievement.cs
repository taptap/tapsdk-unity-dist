using TapSDK.Core;

namespace TapSDK.Achievement
{
    public interface ITapTapAchievement
    {
        void Init(string clientId, TapTapRegionType regionType, TapTapAchievementOptions achievementOptions);

        void Increment(string achievementId, int step);

        void Unlock(string achievementId);

        void ShowAchievements();

        void SetToastEnable(bool enable);

        void RegisterCallBack(ITapAchievementCallback callback);

        void UnRegisterCallBack(ITapAchievementCallback callback);
    }
}