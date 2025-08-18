using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Login.Internal
{
    public class TapTapLoginManager
    {
        private static TapTapLoginManager instance;
        private ITapTapLoginPlatform platformWrapper;

        private TapTapLoginManager()
        {
            platformWrapper = BridgeUtils.CreateBridgeImplementation(typeof(ITapTapLoginPlatform),
                "TapSDK.Login") as ITapTapLoginPlatform;
        }

        public static TapTapLoginManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapTapLoginManager();
                }
                return instance;
            }
        }

        public void Init(string clientId, TapTapRegionType regionType) => platformWrapper.Init(clientId, regionType);

        public Task<TapTapAccount> Login(string[] scopes) => platformWrapper.Login(scopes);

        public void Logout() => platformWrapper.Logout();

        public Task<TapTapAccount> GetCurrentAccount() => platformWrapper.GetCurrentAccount();
    }
}