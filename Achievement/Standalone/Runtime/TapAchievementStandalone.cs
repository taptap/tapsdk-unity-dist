
using System;
using TapSDK.Achievement.Internal.Util;
using TapSDK.Achievement.Internal.Model;
using TapTap.Achievement.Standalone.Internal;
using TapSDK.Achievement.Internal.Http;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using TapSDK.Core.Standalone;
using TapSDK.Core;
using TapSDK.Achievement.Standalone.Internal;
using TapSDK.Core.Standalone.Internal.Http;
using System.Collections.Concurrent;
using System.Threading;
using TapSDK.Login.Internal;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Achievement.Standalone
{
    public class TapAchievementStandalone : ITapTapAchievement
    {

        private static readonly List<ITapAchievementCallback> callbacks = new List<ITapAchievementCallback>();
        // 当前正在向服务端上报的成就的 UID
        ConcurrentDictionary<string, string> currentUploadingAchivements = new ConcurrentDictionary<string, string>();

        ConcurrentDictionary<string, string> currentUploadFailedAchivements = new ConcurrentDictionary<string, string>();

        private static readonly SemaphoreSlim achievementSemaphoreSlim = new SemaphoreSlim(1, 1);

        public static bool toastEnable = true;
        private static TapTapRegionType currentRegionType;
        public async void Init(string clientId, TapTapRegionType regionType, TapTapAchievementOptions achievementOptions)
        {
            currentRegionType = regionType;
            toastEnable = achievementOptions.enableToast;
            // 
            TapAchievementTracker.Instance.TrackInit();
            await PublicAchievement();
        }

        public async void Increment(string achievementId, int step)
        {
            if (!CheckInitState(achievementId))
            {
                return;
            }
            // check login
            if (!await TapAchievementUtil.CheckAccount())
            {
                NotifyCallbackFailure(
                                     achievementId: achievementId,
                                     errorCode: TapTapAchievementConstants.NOT_LOGGED,
                                     "Currently not logged in, please login first."
                                 );
                return;
            }
            TapAchievementStoreBean bean = new TapAchievementStoreBean(type: 0, achievementId: achievementId, steps: step);
            await TapAchievementStore.Save(bean);
            await PublicAchievement();
        }

        public async void Unlock(string achievementId)
        {
            if (!CheckInitState(achievementId))
            {
                return;
            }            
            // check login
            if (!await TapAchievementUtil.CheckAccount())
            {
                Debug.LogError("TapAchievement Increment achievementId: " + achievementId + " failed, not login");
                NotifyCallbackFailure(
                                     achievementId: achievementId,
                                     errorCode: TapTapAchievementConstants.NOT_LOGGED,
                                     "Currently not logged in, please login first."
                                 );
                return;
            }
            TapAchievementLog.Log("Unlock achievementId");
            TapAchievementStoreBean bean = new TapAchievementStoreBean(type: 1, achievementId: achievementId);
            await TapAchievementStore.Save(bean);
            await PublicAchievement();
        }

        public async void ShowAchievements()
        {
            if (!CheckInitState(""))
            {
                return;
            }
            // check login
            if (!await TapAchievementUtil.CheckAccount())
            {
                Debug.LogError("TapAchievement ShowAchievements failed, not login");
                NotifyCallbackFailure(
                                     achievementId: "",
                                     errorCode: TapTapAchievementConstants.NOT_LOGGED,
                                     "Currently not logged in, please login first."
                                 );
                return;
            }

            string seesionId = Guid.NewGuid().ToString();
            TapAchievementTracker.Instance.TrackStart("showAchievements", seesionId);
            // 打开 web
            string url = TapCoreStandalone.getGatekeeperConfigUrl("achievement_my_list_url");
            Application.OpenURL(url);
            TapAchievementTracker.Instance.TrackSuccess("showAchievements", seesionId);
        }

        public void SetToastEnable(bool enable)
        {
            toastEnable = enable;
        }

        public void RegisterCallBack(ITapAchievementCallback callback)
        {
            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
            }
        }

        public void UnRegisterCallBack(ITapAchievementCallback callback)
        {
            callbacks.Remove(callback);
        }

        private bool UrlExistsUsingSockets(string url)
        {
            if (url.StartsWith("https://")) url = url.Remove(0, "https://".Length);
            try
            {
                System.Net.IPHostEntry ipHost = System.Net.Dns.GetHostEntry(url);// System.Net.Dns.Resolve(url);
                return true;
            }
            catch (System.Net.Sockets.SocketException)
            {
                return false;
            }
        }

        private async Task PublicAchievement()
        {
            currentUploadFailedAchivements.Clear();
            await achievementSemaphoreSlim.WaitAsync();
            List<TapAchievementStoreBean> all = await TapAchievementStore.getAll();
            all?.RemoveAll(x => currentUploadingAchivements.ContainsKey(x.UUID));
            all?.RemoveAll(x => currentUploadFailedAchivements.ContainsKey(x.UUID));
            NetworkReachability internetReachability = Application.internetReachability;
            if (all == null || all.Count <= 0)
            {
                achievementSemaphoreSlim.Release();
                return;
            }
            if (!UrlExistsUsingSockets(TapHttp.HOST_CN))
            {
                NotifyCallbackFailure(
                            achievementId: "",
                            errorCode: TapTapAchievementConstants.NETWORK_ERROR,
                            "The network is currently unavailable"
                        );
                achievementSemaphoreSlim.Release();
                return;
            }

            all.ForEach(x => currentUploadingAchivements.TryAdd(x.UUID, x.AchievementId));
            all.ForEach(async (x) =>
            {
                string seesionId = Guid.NewGuid().ToString();
                try
                {
                    TapAchievementResponseData result = null;
                    switch (x.Type)
                    {
                        case 0:
                            TapAchievementTracker.Instance.TrackStart("increment", seesionId, x.AchievementId);
                            result = await TapAchievementAPi.Increment(x.AchievementId, x.Steps);
                            break;
                        case 1:
                            TapAchievementTracker.Instance.TrackStart("unlock", seesionId, x.AchievementId);
                            result = await TapAchievementAPi.Unlock(x.AchievementId);
                            break;
                    }

                    if (result != null)
                    {
                        await TapAchievementStore.Delete(x.UUID);
                        currentUploadingAchivements.TryRemove(x.UUID, out _);
                        TapAchievementResponseBean normalAchievement = result.Achievement;
                        TapAchievementResponseBean platinumAchievement = result.PlatinumAchievement;
                        if (normalAchievement != null)
                        {
                            TapAchievementResult normalAchievementResult =
                                   new TapAchievementResult(
                                       achievementId: normalAchievement.Id ?? "",
                                       achievementName: normalAchievement.Name ?? "",
                                       achievementType: TapAchievementType.NORMAL,
                                       currentSteps: normalAchievement.CurrentSteps
                                   );
                            if (x.Type == 0)
                            {
                                NotifyCallbackSuccess(code: TapTapAchievementConstants.INCREMENT_SUCCESS, normalAchievementResult);
                                TapAchievementTracker.Instance.TrackSuccess("increment", seesionId, x.AchievementId);
                            }
                            else if (x.Type == 1)
                            {
                                TapAchievementTracker.Instance.TrackSuccess("unlock", seesionId, x.AchievementId);
                            }
                            if (normalAchievement.NewlyUnlocked == true)
                            {
                                NotifyCallbackSuccess(code: TapTapAchievementConstants.UNLOCK_SUCCESS, normalAchievementResult);
                                TapAchievementLog.Log("Unlock success1 toastEnable = " + toastEnable);
                                if (toastEnable)
                                {
                                    TapAchievementToastManager.ShowToast(normalAchievementResult);
                                }
                            }
                        }
                        if (platinumAchievement != null && platinumAchievement.NewlyUnlocked == true)
                        {
                            TapAchievementResult platinumAchievementResult =
                                new TapAchievementResult(
                                    achievementId: platinumAchievement.Id ?? "",
                                    achievementName: platinumAchievement.Name ?? "",
                                    achievementType: TapAchievementType.PLATINUM,
                                    currentSteps: platinumAchievement.CurrentSteps
                                );
                            NotifyCallbackSuccess(code: TapTapAchievementConstants.UNLOCK_SUCCESS, platinumAchievementResult);
                            TapAchievementLog.Log("Unlock success2 toastEnable = " + toastEnable);
                            if (toastEnable)
                            {
                                TapAchievementToastManager.ShowToast(platinumAchievementResult);
                            }
                        }
                    }
                    else
                    {
                        currentUploadingAchivements.TryRemove(x.UUID, out _);
                        currentUploadFailedAchivements.TryAdd(x.UUID, x.AchievementId);
                        switch (x.Type)
                        {
                            case 0:
                                TapAchievementTracker.Instance.TrackFailure("increment", seesionId, x.AchievementId, errorCode: TapTapAchievementConstants.UNKNOWN_ERROR, errorMessage: "Request result is null");
                                break;
                            case 1:
                                TapAchievementTracker.Instance.TrackFailure("unlock", seesionId, x.AchievementId, errorCode: TapTapAchievementConstants.UNKNOWN_ERROR, errorMessage: "Request result is null");
                                break;
                        }
                        // do nothing
                        NotifyCallbackFailure(
                                    achievementId: x.AchievementId,
                                    errorCode: TapTapAchievementConstants.UNKNOWN_ERROR,
                                    "Request result is null"
                                );
                    }
                }
                catch (Exception e)
                {
                    currentUploadFailedAchivements.TryAdd(x.UUID, x.AchievementId);
                    if (e is TapHttpServerException exception)
                    {
                        switch (x.Type)
                        {
                            case 0:
                                TapAchievementTracker.Instance.TrackFailure("increment", seesionId, x.AchievementId, errorCode: (int)exception.ErrorData.Code, errorMessage: exception.Message);
                                break;
                            case 1:
                                TapAchievementTracker.Instance.TrackFailure("unlock", seesionId, x.AchievementId, errorCode: (int)exception.ErrorData.Code, errorMessage: exception.Message);
                                break;
                        }
                        switch (exception.ErrorData.Error)
                        {
                            case TapHttpErrorConstants.ERROR_NOT_FOUND:
                            case TapHttpErrorConstants.ERROR_FORBIDDEN:
                            case TapHttpErrorConstants.ERROR_INVALID_REQUEST:
                                await TapAchievementStore.Delete(x.UUID);
                                currentUploadingAchivements.TryRemove(x.UUID, out _);
                                NotifyCallbackFailure(
                                    achievementId: x.AchievementId,
                                    errorCode: TapTapAchievementConstants.INVALID_REQUEST,
                                    exception.ErrorData.Msg
                                );
                                break;
                            case TapHttpErrorConstants.ERROR_ACCESS_DENIED:
                                await TapAchievementStore.Delete(x.UUID);
                                currentUploadingAchivements.TryRemove(x.UUID, out _);
                                NotifyCallbackFailure(
                                     achievementId: x.AchievementId,
                                     errorCode: TapTapAchievementConstants.ACCESS_DENIED,
                                     exception.ErrorData.Msg
                                 );
                                break;
                            default:
                                NotifyCallbackFailure(
                                    achievementId: x.AchievementId,
                                    errorCode: TapTapAchievementConstants.UNKNOWN_ERROR,
                                    exception.Message
                                );
                                currentUploadingAchivements.TryRemove(x.UUID, out _);
                                // do nothing
                                break;
                        }
                    }
                    else
                    {
                        currentUploadingAchivements.TryRemove(x.UUID, out _);
                        TapAchievementTracker.Instance.TrackFailure("increment", seesionId, x.AchievementId, errorCode: TapTapAchievementConstants.UNKNOWN_ERROR, errorMessage: e.Message);
                        // 没到服务器 全部 80030
                        // do nothing
                        NotifyCallbackFailure(
                                     achievementId: x.AchievementId,
                                     errorCode: TapTapAchievementConstants.UNKNOWN_ERROR,
                                     e.Message
                                 );
                    }
                }
            });
            achievementSemaphoreSlim.Release();
        }

        private void NotifyCallbackSuccess(int code, TapAchievementResult result)
        {
            callbacks.ForEach((x) =>
            {
                x.OnAchievementSuccess(code, result);
            });
        }

        private void NotifyCallbackFailure(string achievementId, int errorCode, string errorMsg)
        {
            callbacks.ForEach((x) =>
            {
                x.OnAchievementFailure(achievementId, errorCode, errorMsg ?? "");
            });
        }

        /// <summary>
        /// 校验初始化参数及区域
        /// </summary>
        /// <returns>是否校验通过</returns>
        private bool CheckInitState(string achievementId)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                NotifyCallbackFailure(
                    achievementId: achievementId,
                    errorCode: TapTapAchievementConstants.NOT_INITIALIZED,
                    "Currently not initialized, please initialize first."
                );
                return false;
            }
            if (currentRegionType == TapTapRegionType.Overseas)
            {
                 NotifyCallbackFailure(
                    achievementId: achievementId,
                    errorCode: TapTapAchievementConstants.REGION_NOT_SUPPORTED,
                    "Current RegionType not supported, only support TapTapRegionType.CN"
                );
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("海外不支持使用成就系统服务", "海外不支持使用成就系统服务");
                return false;
            }
            return true;
        }
    }
}