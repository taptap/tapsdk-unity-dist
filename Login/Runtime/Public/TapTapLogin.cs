using System.Threading.Tasks;
using TapSDK.Core.Internal.Utils;
using TapSDK.Login.Internal;

namespace TapSDK.Login
{
    public class TapTapLogin
    {

        public static readonly string Version = "4.7.2-beta.1";

        public const string TAP_LOGIN_SCOPE_BASIC_INFO = "basic_info";
        public const string TAP_LOGIN_SCOPE_PUBLIC_PROFILE = "public_profile";
        public const string TAP_LOGIN_SCOPE_EMAIL = "email";
        public const string TAP_LOGIN_SCOPE_USER_FRIENDS = "user_friends";

        private static TapTapLogin instance;

        private TapTapLogin()
        {
            
        }

        public static TapTapLogin Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapTapLogin();
                }
                return instance;
            }
        }

        public Task<TapTapAccount> LoginWithScopes(string[] scopes) => TapTapLoginManager.Instance.Login(scopes);

        public void Logout() => TapTapLoginManager.Instance.Logout();

        public Task<TapTapAccount> GetCurrentTapAccount() => TapTapLoginManager.Instance.GetCurrentAccount();
    }
}