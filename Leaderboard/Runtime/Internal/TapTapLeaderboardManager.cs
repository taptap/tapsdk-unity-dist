using System.Collections.Generic;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Leaderboard.Runtime.Internal
{
    public class TapTapLeaderboardManager
    {
        private static TapTapLeaderboardManager _instance;

        private readonly ILeaderboardPlatform _platform;

        private readonly List<ITapTapLeaderboardCallback> _callbacks = new List<ITapTapLeaderboardCallback>();

        public static TapTapLeaderboardManager Instance => _instance ?? (_instance = new TapTapLeaderboardManager());

        private TapTapLeaderboardManager()
        {
            _platform = BridgeUtils.CreateBridgeImplementation(typeof(ILeaderboardPlatform), "TapSDK.Leaderboard")
                as ILeaderboardPlatform;
        }

        public void OpenUserProfile(string openId, string unionId) => _platform.OpenUserProfile(openId, unionId);

        public void OpenLeaderboard(string leaderboardId, string collection) =>
            _platform.OpenLeaderboard(leaderboardId, collection);

        public void SubmitScores(List<SubmitScoresRequest.ScoreItem> scores,
            ITapTapLeaderboardResponseCallback<SubmitScoresResponse> callback)
        {
            _platform.SubmitScores(scores, callback);
        }

        public void LoadLeaderboardScores(
            string leaderboardId,
            string leaderboardCollection,
            string nextPage,
            string periodToken,
            ITapTapLeaderboardResponseCallback<LeaderboardScoreResponse> callback) =>
            _platform.LoadLeaderboardScores(leaderboardId, leaderboardCollection, nextPage, periodToken, callback);

        public void LoadCurrentPlayerLeaderboardScore(string leaderboardId,
            string leaderboardCollection,
            string periodToken,
            ITapTapLeaderboardResponseCallback<UserScoreResponse> callback)
        {
            _platform.LoadCurrentPlayerLeaderboardScore(leaderboardId, leaderboardCollection, periodToken, callback);
        }

        public void LoadPlayerCenteredScores(string leaderboardId,
            string leaderboardCollection,
            string periodToken,
            int? maxCount,
            ITapTapLeaderboardResponseCallback<LeaderboardScoreResponse> callback)
        {
            _platform.LoadPlayerCenteredScores(leaderboardId, leaderboardCollection, periodToken, maxCount, callback);
        }

        public void RegisterLeaderboardCallback(ITapTapLeaderboardCallback callback)
        {
            _platform.RegisterLeaderboardCallback(callback);
        }

        public void UnregisterLeaderboardCallback(ITapTapLeaderboardCallback callback)
        {
            _platform.UnRegisterLeaderboardCallback(callback);
        }

        public void SetShareCallback(ITapTapLeaderboardShareCallback callback) => _platform.SetShareCallback(callback);
    }
}