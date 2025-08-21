using UnityEngine.UI;
using UnityEngine;
using TapSDK.UI;
using TapSDK.Achievement.Standalone.Internal;
using TapSDK.Achievement;
using UnityEngine.EventSystems;

namespace TapTap.Achievement.Standalone
{
    public class TapAchievementToast : BasePanelController
    {
        internal class OpenParams : AbstractOpenPanelParameter
        {
            internal TapAchievementResult data;
        }

        [SerializeField] private Text title;
        [SerializeField] private Text congratulationText;
        [SerializeField] private RawImage icon;
        [SerializeField] private new Animation animation;
        [SerializeField] private EventTrigger eventTrigger;

        private OpenParams param;

        protected override void OnLoadSuccess()
        {
            param = (openParam as OpenParams);
            InitContent();
            animation.Play("TapAchievementToastAC");
            AddEventTriggerListener(eventTrigger, EventTriggerType.PointerClick, OnClick); // 添加点击事件监听
        }

        private void InitContent()
        {
            InitTitle();
        }

        private void InitTitle()
        {
            title.text = param.data.AchievementName;
        }

        private void AddEventTriggerListener(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener((data) => callback.Invoke((BaseEventData)data));
            trigger.triggers.Add(entry);
        }

        private void OnClick(BaseEventData data)
        {
            Debug.Log("Toast clicked!"); // 点击事件处理逻辑
            // 打开成就详情页面
            TapTapAchievement.ShowAchievements();
        }

        private void OnPlayEnd()
        {
            Close();
            TapAchievementToastManager.OnAchievementToastEnded();
        }
    }
}
