namespace TapSDK.Leaderboard
{
    public interface ITapTapLeaderboardResponseCallback<in T>
    {
        /// <summary>
        /// 成功回调
        /// </summary>
        /// <param name="data">返回的数据</param>
        void OnSuccess(T data);

        /// <summary>
        /// 失败回调
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message">错误信息</param>
        void OnFailure(int code, string message);
    }

    // 辅助回调类
    public class TapTapTapTapLeaderboardResponseCallback<T> : ITapTapLeaderboardResponseCallback<T>
    {
        public System.Action<T> OnSuccessAction { get; set; }
        public System.Action<int, string> OnFailureAction { get; set; }

        public void OnSuccess(T data)
        {
            OnSuccessAction?.Invoke(data);
        }

        public void OnFailure(int code, string message)
        {
            OnFailureAction?.Invoke(code, message);
        }
    }
}