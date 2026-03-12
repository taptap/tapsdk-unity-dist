using System;
using System.Collections.Generic;
using TapSDK.Moment.Internal;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Moment
{
    public class TapTapMomentConstants
    {
        public static readonly string TapMomentPageShortCut = "tap://moment/scene/";

        public static readonly string TapMomentPageShortCutKey = "scene_id";
        //
        public static readonly int CALLBACK_CODE_ON_STOP = 500;
        //
        public static readonly int CALLBACK_CODE_ON_RESUME = 600;
        // 动态发布成功
        public static readonly int CALLBACK_CODE_PUBLISH_SUCCESS = 10000;
        // 动态发布失败
        public static readonly int CALLBACK_CODE_PUBLISH_FAIL = 10100;
        // 关闭动态发布页面
        public static readonly int CALLBACK_CODE_PUBLISH_CANCEL = 10200;
        // 获取新消息成功
        public static readonly int CALLBACK_CODE_GET_NOTICE_SUCCESS = 20000;
        // 获取新消息失败
        public static readonly int CALLBACK_CODE_GET_NOTICE_FAIL = 20100;
        // 动态页面打开
        public static readonly int CALLBACK_CODE_MOMENT_APPEAR = 30000;
        // 动态页面关闭
        public static readonly int CALLBACK_CODE_MOMENT_DISAPPEAR = 30100;
        //
        public static readonly int CALLBACK_CODE_INIT_SUCCESS = 40000;
        //
        public static readonly int CALLBACK_CODE_INIT_FAIL = 40100;
        // 取消关闭所有动态界面（弹框点击取消按钮）
        public static readonly int CALLBACK_CODE_ClOSE_CANCEL = 50000;
        // 确认关闭所有动态界面（弹框点击确认按钮）
        public static readonly int CALLBACK_CODE_ClOSE_CONFIRM = 50100;
        // 动态页面内登录成功
        public static readonly int CALLBACK_CODE_LOGIN_SUCCESS = 60000;
    }

    public class TapTapMoment
    {

        public static readonly string Version = "4.9.3";

        // 显示动态页面
        public static void open()
        {
            TapTapMomentManager.Instance.OpenMoment();
        }

        // 显示动态页面
        public static void OpenScene(string sceneId)
        {
            TapTapMomentManager.Instance.OpenScene(sceneId);
        }

        // 发布
        public static void Publish(PublishMetaData publishMetaData)
        {
            TapTapMomentManager.Instance.Publish(publishMetaData);
        }

        // 直接关闭动态窗口，不弹出二次确认框
        public static void Close()
        {
            TapTapMomentManager.Instance.Close();
        }

        // 玩家可以在动态页面退出。 但在特定场景下，游戏可能需要主动关闭动态页面
        // 比如，玩家排位等待结束，准备进入对局时提示玩家关闭动态页面，玩家确认后关闭
        public static void CloseWithTitle(string title, string desc)
        {
            TapTapMomentManager.Instance.CloseWithConfirmWindow(title, desc);
        }

        // 定时调用获取消息通知的接口，有新信息时可以在 TapTap 动态入口显示小红点，提醒玩家查看新动态。
        public static void FetchNotification()
        {
            TapTapMomentManager.Instance.FetchNotification();
        }

        public static void SetCallback(Action<int, string> callback)
        {
            TapTapMomentManager.Instance.SetCallback(callback);
        }
    }
}
