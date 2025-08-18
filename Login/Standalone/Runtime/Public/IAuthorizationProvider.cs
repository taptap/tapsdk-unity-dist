using System.Threading.Tasks;

namespace TapSDK.Login.Standalone
{
    public interface IAuthorizationProvider
    {
        Task<AccessToken> Authorize(string[] scopes = null);
    }
}