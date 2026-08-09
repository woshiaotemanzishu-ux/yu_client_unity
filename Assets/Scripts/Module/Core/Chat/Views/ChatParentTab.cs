using System;
using Shenxiao.Generated.UI.Chat;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Chat
{
    /// <summary>
    /// 聊天频道页签按钮项(对标老客户端 chat/ChatParentTab.ts):一个频道标签——背景 _Image1 + 频道名 labelDisplay +
    /// 选中下划线/高亮 _fg_line。SetData 写频道名 + 绑点击回调(index);SetSelected 切 _fg_line 选中态。
    ///
    /// 降级:ChatModel(频道未读/可见性)未移植 → 仅做最小可用(文字 + 点击切换 + 选中高亮);未读红点/置灰待后端。
    /// 由 ChatParentView 克隆铺设(世界/仙宗/队伍/跨服/活动/阵营/海域/系统)。
    /// </summary>
    public sealed class ChatParentTab : ChatParentTabBind
    {
        private int _index;
        private Action<int> _onClick;
        private bool _clickBound;

        /// <summary>填频道页签(对标 SetData):写频道名 + 记录 index + 一次性绑点击。</summary>
        public void SetData(string label, int index, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;

            if (labelDisplay != null) labelDisplay.text = label ?? string.Empty;

            if (!_clickBound)
            {
                Image btn = _Image1;
                if (btn == null) btn = GetComponentInChildren<Image>(true);
                if (btn != null)
                {
                    btn.raycastTarget = true;
                    UIUtil.AddClick(btn, () => _onClick?.Invoke(_index));
                    _clickBound = true;
                }
            }
        }

        /// <summary>选中态对齐老端：选中显示 111 宽背景并用黑字，未选中隐藏背景并用白字。</summary>
        public void SetSelected(bool on)
        {
            if (_Image1 != null)
            {
                RectTransform backgroundRect = _Image1.rectTransform;
                Vector2 size = backgroundRect.sizeDelta;
                size.x = 111f;
                backgroundRect.sizeDelta = size;
                _Image1.gameObject.SetActive(on);
            }

            if (labelDisplay != null) labelDisplay.color = on ? Color.black : Color.white;
            if (_fg_line != null) _fg_line.gameObject.SetActive(false);
        }
    }
}
