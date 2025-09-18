using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TapSDK.Leaderboard.Runtime.Internal;

namespace TapSDK.Leaderboard
{
    /// <summary>
    /// TapTap排行榜SDK主类
    /// </summary>
    public static class TapTapLeaderboard
    {
        /// <summary>
        /// SDK版本号
        /// </summary>
        public static readonly string Version = "4.8.2";

        /// <summary>
        /// 打开排行榜页面
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="collection">排行榜集合</param>
        public static void OpenLeaderboard(string leaderboardId, string collection)
        {
            TapTapLeaderboardManager.Instance.OpenLeaderboard(leaderboardId, collection);
        }

        /// <summary>
        /// 打开用户资料页面
        /// </summary>
        /// <param name="openId">用户OpenID</param>
        /// <param name="unionId">用户UnionId</param>
        public static void ShowTapUserProfile(string openId, string unionId)
        {
            TapTapLeaderboardManager.Instance.OpenUserProfile(openId, unionId);
        }

        /// <summary>
        /// 批量提交用户排行榜分数（一次最多提交5个分数）
        /// </summary>
        /// <param name="scores">分数列表</param>
        /// <returns>提交结果</returns>
        public static Task<SubmitScoresResponse> SubmitScores(List<SubmitScoresRequest.ScoreItem> scores)
        {
            return TapTapLeaderboardManager.Instance.SubmitScores(scores);
        }

        /// <summary>
        /// 获取排行榜数据
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="leaderboardCollection">排行榜集合</param>
        /// <param name="nextPage">下一页标识</param>
        /// <param name="periodToken">周期标识</param>
        /// <returns>排行榜分数结果</returns>
        public static Task<LeaderboardScoreResponse> LoadLeaderboardScores(
            string leaderboardId,
            string leaderboardCollection,
            string nextPage,
            string periodToken)
        {
            return TapTapLeaderboardManager.Instance.LoadLeaderboardScores(
                leaderboardId,
                leaderboardCollection,
                nextPage,
                periodToken
            );
        }

        /// <summary>
        /// 单查用户排行榜数据
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="leaderboardCollection">排行榜集合</param>
        /// <param name="periodToken">周期标识</param>
        /// <returns>用户分数结果</returns>
        public static Task<UserScoreResponse> LoadCurrentPlayerLeaderboardScore(
            string leaderboardId,
            string leaderboardCollection,
            string periodToken)
        {
            return TapTapLeaderboardManager.Instance.LoadCurrentPlayerLeaderboardScore(
                leaderboardId,
                leaderboardCollection,
                periodToken
            );
        }

        /// <summary>
        /// 查询用户相近用户成绩
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="leaderboardCollection">排行榜集合</param>
        /// <param name="periodToken">周期标识</param>
        /// <param name="maxCount">最大数量，-1表示不限制</param>
        /// <returns>排行榜分数结果</returns>
        public static Task<LeaderboardScoreResponse> LoadPlayerCenteredScores(
            string leaderboardId,
            string leaderboardCollection,
            string periodToken,
            int? maxCount)
        {
            return TapTapLeaderboardManager.Instance.LoadPlayerCenteredScores(
                leaderboardId,
                leaderboardCollection,
                periodToken,
                maxCount
            );
        }

        /// <summary>
        /// 注册排行榜回调
        /// </summary>
        /// <param name="callback">回调</param>
        public static void RegisterLeaderboardCallback(ITapTapLeaderboardCallback callback)
        {
            TapTapLeaderboardManager.Instance.RegisterLeaderboardCallback(callback);
        }

        /// <summary>
        /// 取消注册排行榜回调
        /// </summary>
        /// <param name="callback">回调</param>
        public static void UnregisterLeaderboardCallback(ITapTapLeaderboardCallback callback)
        {
            TapTapLeaderboardManager.Instance.UnregisterLeaderboardCallback(callback);
        }

        /// <summary>
        /// 设置分享回调
        /// </summary>
        /// <param name="callback">回调</param>
        public static void SetShareCallback(ITapTapLeaderboardShareCallback callback)
        {
            TapTapLeaderboardManager.Instance.SetShareCallback(callback);
        }
    }
}