using System.Threading.Tasks;
using TapSDK.Login.Standalone;

namespace TapSDK.Login.Internal
{
    public class AuthorizationProviderImpl : IAuthorizationProvider
    {
        public Task<AccessToken> Authorize(string[] scopes = null)
        {
            return TapLoginStandaloneImpl.Instance.Authorize(scopes);
        }
    }
}