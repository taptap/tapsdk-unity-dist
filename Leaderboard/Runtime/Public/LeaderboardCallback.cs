using System;

namespace TapSDK.Leaderboard
{
    /// <summary>
    /// 排行榜通用回调接口
    /// </summary>
    public interface ITapTapLeaderboardCallback
    {
        /// <summary>
        /// 回调
        /// </summary>
        void OnLeaderboardResult(int code, string message);
    }
    
    public class TapTapLeaderboardCallback : ITapTapLeaderboardCallback
    {
        public Action<int, string> OnCallbackAction { get; set; }

        public void OnLeaderboardResult(int code, string message)
        {
            OnCallbackAction?.Invoke(code, message);
        }
    }
}