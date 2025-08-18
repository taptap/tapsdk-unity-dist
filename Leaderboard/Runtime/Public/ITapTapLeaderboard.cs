using System.Collections.Generic;

namespace TapSDK.Leaderboard
{
    public interface ITapTapLeaderboard
    {
        /// <summary>
        /// 打开用户资料页面
        /// </summary>
        /// <param name="openId">用户OpenID</param>
        void OpenUserProfile(string openId);

        /// <summary>
        /// 打开排行榜页面
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="collection">排行榜集合</param>
        void OpenLeaderboard(string leaderboardId, string collection);

        /// <summary>
        /// 批量提交用户排行榜分数（一次最多提交5个分数）
        /// </summary>
        /// <param name="scores">分数列表</param>
        void SubmitScores(List<SubmitScoresRequest.ScoreItem> scores);

        /// <summary>
        /// 获取排行榜数据
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="leaderboardCollection">排行榜集合</param>
        /// <param name="nextPage">下一页标识</param>
        /// <param name="periodToken">周期标识</param>
        void LoadLeaderboardScores(
            string leaderboardId,
            string leaderboardCollection,
            string nextPage = null,
            string periodToken = null);

        /// <summary>
        /// 单查用户排行榜数据
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="leaderboardCollection">排行榜集合</param>
        /// <param name="periodToken">周期标识</param>
        void LoadCurrentPlayerLeaderboardScore(
            string leaderboardId,
            string leaderboardCollection,
            string periodToken = null);

        /// <summary>
        /// 查询用户相近用户成绩
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="leaderboardCollection">排行榜集合</param>
        /// <param name="periodToken">周期标识</param>
        /// <param name="maxCount">最大数量，-1表示不限制</param>
        void LoadPlayerCenteredScores(
            string leaderboardId,
            string leaderboardCollection,
            string periodToken = null,
            int? maxCount = null);

        /// <summary>
        /// 设置分享回调
        /// </summary>
        /// <param name="callback">回调</param>
        void SetShareCallback(ITapTapLeaderboardShareCallback callback);
    }
}