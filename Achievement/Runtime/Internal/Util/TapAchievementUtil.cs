using System;
using System.Threading.Tasks;
using TapSDK.Login;

namespace TapSDK.Achievement.Internal.Util
{
    public class TapAchievementUtil
    {
        private TapAchievementUtil()
        {

        }

        public static bool CheckInit()
        {
            return TapAchievementInitTask.IsInit == true;
        }

        public async static Task<bool> CheckAccount()
        {
            TapTapAccount tapAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
            return tapAccount != null && !string.IsNullOrEmpty(tapAccount.openId);
        }
    }
}
