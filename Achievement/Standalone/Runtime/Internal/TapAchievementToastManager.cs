using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TapSDK.Achievement.Internal.Model;
using TapSDK.Achievement.Internal.Util;
using TapSDK.UI;
using TapTap.Achievement.Standalone;

namespace TapSDK.Achievement.Standalone.Internal
{
    public class TapAchievementToastManager
    {
        private static bool isShowingToast = false;
        private static List<TapAchievementResult> toastingAchievements = new List<TapAchievementResult>();

        public static void ShowToast(TapAchievementResult bean)
        {
            if (!TapAchievementStandalone.toastEnable)
            {
                return;
            }

            TapAchievementLog.Log("ShowToast called = " + JsonConvert.SerializeObject(bean));
            if (isShowingToast)
            {
                toastingAchievements.Add(bean);
            }
            else
            {
                isShowingToast = true;
                var openParams = new TapAchievementToast.OpenParams() { data = bean };
                UIManager.Instance.OpenUI<TapAchievementToast>("Prefabs/TapAchievementToast", openParams);
            }
        }

        internal static void OnAchievementToastEnded()
        {
            if (toastingAchievements.Count > 0)
            {
                var openParams = new TapAchievementToast.OpenParams() { data = toastingAchievements[0] };
                toastingAchievements.RemoveAt(0);
                UIManager.Instance.OpenUI<TapAchievementToast>("Prefabs/TapAchievementToast", openParams);
            }
            else
            {
                isShowingToast = false;
            }
        }
    }
}