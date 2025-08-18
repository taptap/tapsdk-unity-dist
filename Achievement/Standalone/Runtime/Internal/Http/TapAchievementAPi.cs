using System.Threading.Tasks;
using TapSDK.Login;
using System;
using TapSDK.Achievement.Internal.Model;
using TapSDK.Achievement.Internal.Util;
using TapSDK.Login.Internal;
using TapSDK.Core.Standalone.Internal.Http;
using TapSDK.Core.Standalone;

namespace TapSDK.Achievement.Internal.Http
{
    public static class TapAchievementAPi
    {
        private static readonly TapHttp tapHttp = TapHttp
            .NewBuilder("TapAchievement", TapTapAchievement.Version)
            .Build();

        public static async Task<TapAchievementResponseData> Unlock(string achievementId)
        {
            TapAchievementUnlockRequest body = new TapAchievementUnlockRequest(achievementId: achievementId);
            TapAchievementLog.Log("Increment achievementId = " + achievementId);
            string path = "achievement/v1/unlock";
            TapHttpResult<TapAchievementResponseData> response = await tapHttp.PostJsonAsync<TapAchievementResponseData>(path: path, json: body, enableAuthorization: true);

            TapAchievementLog.Log("Increment response = " + response);
            if (response.IsSuccess)
            {
                return response.Data;
            }
            else
            {
                throw response.HttpException;
            }
        }

        public static async Task<TapAchievementResponseData> Increment(string achievementId, int steps)
        {
            TapAchievementIncrementRequest body = new TapAchievementIncrementRequest(achievementId: achievementId, steps: steps);
            string path = "achievement/v1/increment";
            TapHttpResult<TapAchievementResponseData> response = await tapHttp.PostJsonAsync<TapAchievementResponseData>(
                path: path,
                json: body,
                enableAuthorization: true
            );
            TapAchievementLog.Log("Increment response = " + response);
            if (response.IsSuccess)
            {
                return response.Data;
            }
            else
            {
                throw response.HttpException;
            }
        }
    }
}