using System.Threading.Tasks;
using System;
using TapSDK.Core.Standalone;

namespace TapSDK.Login.Internal
{
#if UNITY_STANDALONE_WIN
    internal class TapLoginClientBridge
    {
        private static TaskCompletionSource<TapLoginResponseByTapClient> taskCompletionSource;

        public static async Task<TapLoginResponseByTapClient> LoginWithScopesAsync(string[] scopes, string responseType, string redirectUri,
    string codeChallenge, string state, string codeChallengeMethod, string versonCode, string sdkUa, string info)
        {
            taskCompletionSource = new TaskCompletionSource<TapLoginResponseByTapClient>();
            await Task.Run(() =>
                {
                    try
                    {
                        bool isStartSuccess = TapClientStandalone.StartLoginWithScopes(scopes, responseType, redirectUri, codeChallenge, state, codeChallengeMethod, versonCode, sdkUa, info, LoginDelegate);
                        if (!isStartSuccess)
                        {
                            taskCompletionSource.TrySetResult(new TapLoginResponseByTapClient("发起授权失败，请确认 Tap 客户端是否正常运行"));
                            taskCompletionSource = null;
                        }
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.TrySetResult(new TapLoginResponseByTapClient(false, e.Message));
                        taskCompletionSource = null;
                    }
                }
            );
            return await taskCompletionSource.Task;
        }

        private static void LoginDelegate(bool isCancel, string redirectUri)
        {
            if (taskCompletionSource != null)
            {
                taskCompletionSource.TrySetResult(new TapLoginResponseByTapClient(isCancel, redirectUri));
                taskCompletionSource = null;
            }
        }

        // 使用客户端登录结果返回值
        public class TapLoginResponseByTapClient
        {

            public bool isCancel = false;

            public string redirectUri;

            public bool isFail = false;

            public string errorMsg;

            public TapLoginResponseByTapClient(bool isCancel, string redirctUri)
            {
                this.redirectUri = redirctUri;
                this.isCancel = isCancel;
            }

            public TapLoginResponseByTapClient(string errorMsg)
            {
                isFail = true;
                isCancel = false;
                this.errorMsg = errorMsg;
            }


        }

        // 使用客户端登录结果回调
        public interface TapLoginCallbackWithTapClient
        {
            void onSuccess(TapLoginResponseByTapClient response);

            void onFailure(string error);

            void onCancel();
        }


    }
#endif
}
