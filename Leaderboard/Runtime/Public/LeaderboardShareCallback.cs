using System;

namespace TapSDK.Leaderboard
{
    /// <summary>
    /// 排行榜分享回调接口
    /// </summary>
    public interface ITapTapLeaderboardShareCallback
    {
        /// <summary>
        /// 分享成功回调
        /// </summary>
        void OnShareSuccess(string localPath);

        /// <summary>
        /// 分享失败回调
        /// </summary>
        void OnShareFailed(Exception error);
    }
    
    public class TapTapLeaderboardShareCallback : ITapTapLeaderboardShareCallback
    {
        public Action<string> OnShareSuccessAction { get; set; }
        public Action<Exception> OnShareFailedAction { get; set; }

        public void OnShareSuccess(string localPath)
        {
            OnShareSuccessAction?.Invoke(localPath);
        }

        public void OnShareFailed(Exception error)
        {
            OnShareFailedAction?.Invoke(error);
        }
    }
}