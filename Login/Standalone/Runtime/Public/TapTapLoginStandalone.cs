using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Login.Internal;
using TapSDK.Core.Standalone;
using System.Diagnostics;

namespace TapSDK.Login.Standalone
{
    public class TapTapLoginStandalone: ITapTapLoginPlatform
    {
        
        public void Init(string clientId, TapTapRegionType regionType)
        {
            TapLoginStandaloneImpl.Instance.Init(clientId, regionType);
        }

        public Task<TapTapAccount> Login(string[] scopes)
        {
             if(!TapCoreStandalone.CheckInitState()){
                var defaultTcs = new TaskCompletionSource<TapTapAccount>();
                defaultTcs.TrySetResult(null);
                return defaultTcs.Task;
            }
            return TapLoginStandaloneImpl.Instance.Login(scopes);
        }

        public void Logout()
        {
            if(!TapCoreStandalone.CheckInitState()){
                return;
            }
            TapLoginStandaloneImpl.Instance.Logout();
        }

        public Task<TapTapAccount> GetCurrentAccount()
        {
            if(!TapCoreStandalone.CheckInitState()){
                var defaultTcs = new TaskCompletionSource<TapTapAccount>();
                defaultTcs.TrySetResult(null);
                return defaultTcs.Task;
            }
            return TapLoginStandaloneImpl.Instance.GetCurrentAccount();
        }
    }

    public class TapTapLoginOpenIDProvider: IOpenIDProvider {
        public string GetOpenID() {
            return TapLoginStandaloneImpl.Instance.GetCurrentAccount().Result?.openId;
        }
    }
}