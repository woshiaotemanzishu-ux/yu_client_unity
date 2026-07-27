using System;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// HudNotification 唯一通知模板。图标、红点、数字角标、特效挂点和点击语义均随状态填充。
    /// </summary>
    public sealed class MainUINotificationItem : MonoBehaviour
    {
        public Image Icon;
        public Image RedDot;
        public Image CountBadge;
        public TextMeshProUGUI CountLabel;
        public RectTransform EffectAnchor;

        private string _iconPath;

        public void SetData(string iconPath, bool showRedDot, int count, Action onClick)
        {
            _iconPath = iconPath;
            if (Icon != null)
            {
                UIUtil.ClearClicks(Icon);
                UIUtil.AddClick(Icon, onClick);
                _ = LoadIconAsync(iconPath);
            }

            if (RedDot != null) RedDot.gameObject.SetActive(showRedDot && count <= 1);
            bool showCount = count > 1;
            if (CountBadge != null) CountBadge.gameObject.SetActive(showCount);
            if (CountLabel != null) CountLabel.text = showCount ? count.ToString() : string.Empty;
            if (EffectAnchor != null) EffectAnchor.gameObject.SetActive(false);
        }

        private async System.Threading.Tasks.Task LoadIconAsync(string path)
        {
            if (Icon == null || string.IsNullOrEmpty(path)) return;
            await ResManager.SetImageAsync(Icon, path, nativeSize: false);
            if (this == null || path == _iconPath) return;

            // 同一模板在短时间内复用为另一类消息时，较早的异步加载可能后返回；
            // 重新以最新路径覆盖，避免图标与点击语义错位。
            _ = LoadIconAsync(_iconPath);
        }
    }
}
