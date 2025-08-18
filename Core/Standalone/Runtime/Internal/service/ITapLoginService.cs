using System.Threading.Tasks;

namespace TapSDK.Core.Standalone.Internal.Service
{
    public interface ITapLoginService
    {
        string ObtainAuthorizationAsync(string url, string method);
    }
}