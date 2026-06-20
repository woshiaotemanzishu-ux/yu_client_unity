using System;
using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 缘分(婚姻)-顶部页签按钮项(对标老客户端 marriage/MarriageBaseBtn.ts):
    /// 一个可点击的 Tab 按钮——背景(_img_bg)+ 选中高亮(_img_bg2,选中时可见)+ 名称图(_img_name)+ 页签文字(tab_name)+ 红点(_reddot)。
    /// 老端逻辑:SetData(data,index) 写 tab_name.text=data.label 并存 index;点击 con → 校验角色等级(open_lv)后切换选中态并 Fire(TAB_BTN_CLICK,index);
    /// SetSelect(bool) 切 _img_bg2.visible 并改字色/描边;SetRedDot(bool) 切 _reddot.visible;监听 TAB_BTN_CLICK 互斥取消其它页签选中。
    ///
    /// 降级:MarriageModel(TAB_BTN_CLICK 事件/互斥)、RoleManager 等级校验(open_lv)、SysInfo 提示、字色/描边主题均未移植 →
    /// 本类只做最小可用:SetData 写文字 + 绑定点击回调(回调里由父级 View 负责互斥与等级校验);SetSelected 仅切 _img_bg2 高亮;
    /// OnInit 先隐藏红点 _reddot。Null-guard 每处访问。
    /// </summary>
    public sealed class MarriageBaseBtn : MarriageBaseBtnBind
    {
        private int _index;
        private Action<int> _onClick;

        protected override void OnInit()
        {
            HideReds();
        }

        /// <summary>红点 _reddot(老端 SetRedDot),数据未移植先隐藏。</summary>
        private void HideReds()
        {
            HideNode(_reddot);
        }

        /// <summary>
        /// 设置页签数据(对标老端 SetData + InitEvents 的点击绑定):
        /// 写入页签文字 tab_name,记录 index,并把按钮背景 _img_bg 的点击绑到 onClick(index)。
        /// </summary>
        public void SetData(string label, int index, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;

            if (tab_name != null)
            {
                tab_name.text = label ?? string.Empty;
            }

            // 老端点击挂在容器 con 上;con 为 RectTransform 非 Image,用背景图 _img_bg 承接点击。
            Image btn = _img_bg;
            if (btn == null && con != null) btn = con.GetComponentInChildren<Image>(true);
            if (btn != null)
            {
                btn.raycastTarget = true;
                UIUtil.AddClick(btn, () =>
                {
                    GameLog.Info("Marriage", "点击页签[{0}] → 待对接(回调 index={1})", label, _index);
                    _onClick?.Invoke(_index);
                });
            }
        }

        /// <summary>设置选中态(对标老端 SetSelect):切换选中高亮 _img_bg2 的显隐。</summary>
        public void SetSelected(bool on)
        {
            if (_img_bg2 != null) _img_bg2.gameObject.SetActive(on);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
