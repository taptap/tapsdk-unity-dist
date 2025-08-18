using TapSDK.Core.Internal.Log;


namespace TapSDK.Core.Internal.Utils
{
    public class TapVerifyInitStateUtils
    {
    
        public static void ShowVerifyErrorMsg(string error, string errorMsg)
        {
            if (error != null || error.Length > 0)
            {
                TapMessage.ShowMessage(error, TapMessage.Position.bottom, TapMessage.Time.twoSecond);
            }
            if (errorMsg != null && errorMsg.Length > 0)
            {
                TapLog.Error(errorMsg);
            }
        }
    }
}