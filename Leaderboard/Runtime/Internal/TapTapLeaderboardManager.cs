using System.Collections.Generic;
using System.Threading.Tasks;
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

        public Task<SubmitScoresResponse> SubmitScores(List<SubmitScoresRequest.ScoreItem> scores)
        {
            return _platform.SubmitScores(scores);
        }

        public Task<LeaderboardScoreResponse> LoadLeaderboardScores(
            string leaderboardId,
            string leaderboardCollection,
            string nextPage,
            string periodToken)
        {
            return _platform.LoadLeaderboardScores(leaderboardId, leaderboardCollection, nextPage, periodToken);
        }

        public Task<UserScoreResponse> LoadCurrentPlayerLeaderboardScore(string leaderboardId,
            string leaderboardCollection,
            string periodToken)
        {
            return _platform.LoadCurrentPlayerLeaderboardScore(leaderboardId, leaderboardCollection, periodToken);
        }

        public Task<LeaderboardScoreResponse> LoadPlayerCenteredScores(string leaderboardId,
            string leaderboardCollection,
            string periodToken,
            int? maxCount)
        {
            return _platform.LoadPlayerCenteredScores(leaderboardId, leaderboardCollection, periodToken, maxCount);
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