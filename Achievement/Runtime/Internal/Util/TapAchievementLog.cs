using System;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Achievement.Internal.Util
{
    public class TapAchievementLog
    {
        private static readonly TapLog log = new TapLog(module: "Achievement");
        
        public static void Log(string message)
        {
            log.Log(message);
        }

        public static void Log(Exception e)
        {
            log.Error(e.Message);
        }
    }
}