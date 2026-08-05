using System;
using Shenxiao.Generated.UI.Role;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 主动技能图标项(对标老客户端 role/SkillInitiativeItem.ts)。
    /// 节点结构和尺寸由 RoleModule.prefab 提供，代码只绑定 SkillVo、状态和点击。
    /// </summary>
    public sealed class SkillInitiativeItem : SkillInitiativeItemBind
    {
        private Action _onClick;

        protected override void OnInit()
        {
            // 老端图标项的等级文本始终 visible=false，等级只显示在下方详情。
            if (_lb_level != null) _lb_level.gameObject.SetActive(false);
            if (_img_select != null) _img_select.gameObject.SetActive(false);
            if (_img_lock != null) _img_lock.gameObject.SetActive(false);
            if (_reddot != null) _reddot.gameObject.SetActive(false);
            if (_img_black != null) _img_black.gameObject.SetActive(false);

            BindClick(_img_icon);
            BindClick(_img_lock);
        }

        public void SetClick(Action onClick) => _onClick = onClick;

        public void SetData(SkillVo vo)
        {
            if (vo == null)
            {
                SetLevel("");
                SetLocked(false);
                SetIcon("0");
                return;
            }

            SetLevel("");
            SetLocked(vo.Locked);
            // 老端即使未解锁也先显示该槽的一阶技能图标，锁罩单独负责状态。
            SetIcon(vo.DisplayIcon);
        }

        public void SetLevel(string level)
        {
            if (_lb_level != null) _lb_level.gameObject.SetActive(false);
            if (_lb_level != null) _lb_level.text = level ?? "";
        }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }

        public void SetIcon(string skillIcon)
        {
            if (_img_icon == null) return;
            _img_icon.enabled = true;
            _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetSkillIcon(skillIcon), nativeSize: false);
        }

        public void SetLocked(bool locked)
        {
            if (_img_lock != null) _img_lock.gameObject.SetActive(locked);
            if (_img_black != null) _img_black.gameObject.SetActive(locked);
        }

        private void BindClick(Image image)
        {
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, () => _onClick?.Invoke());
        }
    }
}
