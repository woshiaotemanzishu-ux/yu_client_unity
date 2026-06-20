using System;
using Shenxiao.Generated.UI.Role;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 主动技能项(对标老客户端 role/SkillInitiativeItem.ts):技能图标(_img_icon)+ 等级(_lb_level)+ 选中(_img_select)/锁(_img_lock/_img_black)/红点(_reddot),点击查看/选择。
    ///
    /// SetLevel / SetSelected / SetLocked / SetClick。降级:技能图标/数据(SkillUIModel)未移植 → 等级/选中/点击可用,锁/红点 OnInit 隐藏。由界面克隆。
    /// </summary>
    public sealed class SkillInitiativeItem : SkillInitiativeItemBind
    {
        private Action _onClick;

        protected override void OnInit()
        {
            if (_img_select != null) _img_select.gameObject.SetActive(false);
            if (_img_lock != null) _img_lock.gameObject.SetActive(false);
            if (_reddot != null) _reddot.gameObject.SetActive(false);
            if (_img_black != null) _img_black.gameObject.SetActive(false);
            BindClick(_img_icon, () => { if (_onClick != null) _onClick(); });
        }

        public void SetClick(Action onClick) { _onClick = onClick; }
        public void SetLevel(string level) { if (_lb_level != null) _lb_level.text = level ?? ""; }
        public void SetSelected(bool sel) { if (_img_select != null) _img_select.gameObject.SetActive(sel); }

        public void SetLocked(bool locked)
        {
            if (_img_lock != null) _img_lock.gameObject.SetActive(locked);
            if (_img_black != null) _img_black.gameObject.SetActive(locked);
        }

        private void BindClick(Image img, Action onClick)
        {
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
