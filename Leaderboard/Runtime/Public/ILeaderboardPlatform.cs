using System.Collections.Generic;
using System.Threading.Tasks;

namespace TapSDK.Leaderboard
{
    /// <summary>
    /// 排行榜平台接口
    /// </summary>
    public interface ILeaderboardPlatform
    {
        /// <summary>
        /// 打开排行榜界面
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="collection">排行榜集合类型</param>
        void OpenLeaderboard(string leaderboardId, string collection);

        /// <summary>
        /// 打开用户资料界面
        /// </summary>
        /// <param name="openId">用户OpenID</param>
        /// <param name="unionId">用户UnionID</param>
        void OpenUserProfile(string openId, string unionId);

        /// <summary>
        /// 提交分数
        /// </summary>
        /// <param name="scores">分数列表</param>
        /// <returns>提交结果</returns>
        Task<SubmitScoresResponse> SubmitScores(List<SubmitScoresRequest.ScoreItem> scores);

        /// <summary>
        /// 加载排行榜分数
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="leaderboardCollection">排行榜集合类型</param>
        /// <param name="nextPage">下一页标识</param>
        /// <param name="periodToken">周期标识</param>
        /// <returns>排行榜分数结果</returns>
        Task<LeaderboardScoreResponse> LoadLeaderboardScores(
            string leaderboardId,
            string leaderboardCollection,
            string nextPage,
            string periodToken);

        /// <summary>
        /// 加载当前玩家排行榜分数
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="leaderboardCollection">排行榜集合类型</param>
        /// <param name="periodToken">周期标识</param>
        /// <returns>用户分数结果</returns>
        Task<UserScoreResponse> LoadCurrentPlayerLeaderboardScore(string leaderboardId,
            string leaderboardCollection,
            string periodToken);

        /// <summary>
        /// 加载以玩家为中心的分数
        /// </summary>
        /// <param name="leaderboardId">排行榜ID</param>
        /// <param name="leaderboardCollection">排行榜集合类型</param>
        /// <param name="periodToken">周期标识</param>
        /// <param name="maxCount">最大数量</param>
        /// <returns>排行榜分数结果</returns>
        Task<LeaderboardScoreResponse> LoadPlayerCenteredScores(string leaderboardId,
            string leaderboardCollection,
            string periodToken,
            int? maxCount);

        /// <summary>
        /// 设置分享回调
        /// </summary>
        /// <param name="callback">回调</param>
        void SetShareCallback(ITapTapLeaderboardShareCallback callback);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        void RegisterLeaderboardCallback(ITapTapLeaderboardCallback callback);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        void UnRegisterLeaderboardCallback(ITapTapLeaderboardCallback callback);
    }
}