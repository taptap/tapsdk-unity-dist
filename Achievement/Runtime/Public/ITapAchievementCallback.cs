namespace TapSDK.Achievement
{
    public interface ITapAchievementCallback
    {
        void OnAchievementSuccess(int code, TapAchievementResult result);

        void OnAchievementFailure(string achievementId, int errorCode, string errorMsg);
    }
}