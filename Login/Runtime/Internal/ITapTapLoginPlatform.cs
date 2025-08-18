using System.Threading.Tasks;
using TapSDK.Core;

namespace TapSDK.Login.Internal
{
    public interface ITapTapLoginPlatform
    {
        void Init(string clientId, TapTapRegionType regionType);
        Task<TapTapAccount> Login(string[] scopes);
        
        void Logout();
        
        Task<TapTapAccount> GetCurrentAccount();
    }
}