using Shenxiao.Generated.UI.Role;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Skill;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 主动技能子项/详情(对标老客户端 role/SkillInitiativeSubItem.ts):技能详情 + 穿戴按钮(skill_wear_btn)+ 性别立绘(_img_girl/_img_boy)
    /// + 当前/下级效果滚动列表。
    ///
    /// **RoleFlow 技能"主动技能"tab 容器**(技能成长线轮3 接入):RoleModule.prefab 顶层这份实例是 720×997 满页节点
    /// (对标老端整页,非小图标),内嵌 _tpl_SkillInitiativeItem(单个技能图标槽,老端本是 _group_item 横排多技能可切换,
    /// 但 Unity 快照只捕获了这一枚模板、_group_item 是空容器且无 LayoutGroup——按"布局归 prefab"约束不在 C# 里
    /// 臆造多槽坐标,本轮只原地复用这一枚槽位展示 <see cref="SkillManager.ShortcutList"/> 首个技能(纯展示,无升级按钮,
    /// 对标老端 tab0 无 _gp_level_up)。多技能横向切换需要美术/UiCreator 补 Layout 后再扩,留 TODO。
    ///
    /// 降级:穿戴协议(SkillUIModel 穿戴)未移植 → skill_wear_btn 点击打 TODO。
    /// </summary>
    public sealed class SkillInitiativeSubItem : SkillInitiativeSubItemBind
    {
        private SkillInitiativeItem _iconItem;

        protected override void OnInit()
        {
            BindBtn(skill_wear_btn, () => GameLog.Info("Role", "穿戴主动技能 → 待对接 SkillUIModel 穿戴协议"));

            if (_tpl_SkillInitiativeItem != null)
            {
                _tpl_SkillInitiativeItem.SetActive(true); // 原地复用为"当前技能"单槽展示(见类头注释)
                _iconItem = _tpl_SkillInitiativeItem.GetComponent<SkillInitiativeItem>();
            }
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_SKILL_LIST_UPDATED, Refresh);
            EventDispatcher.On<int>(GlobalEvent.EVT_SKILL_LEVEL_UP, OnLevelUp);
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SKILL_LIST_UPDATED, Refresh);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SKILL_LEVEL_UP, OnLevelUp);
        }

        private void OnLevelUp(int skillId) => Refresh();

        private void Refresh()
        {
            IReadOnlyList<SkillVo> list = SkillManager.Instance.ShortcutList;
            SkillVo vo = list != null && list.Count > 0 ? list[0] : null;

            if (vo == null)
            {
                if (_lb_name != null) _lb_name.text = "";
                if (_lb_level != null) _lb_level.text = "";
                if (_lb_content_now != null) _lb_content_now.text = "";
                if (_lb_content_next != null) _lb_content_next.text = "";
                if (_iconItem != null) _iconItem.SetIcon("0");
                return;
            }

            if (_lb_name != null) _lb_name.text = vo.GetName();
            if (_lb_level != null) _lb_level.text = vo.Level.ToString();
            if (_lb_content_now != null) _lb_content_now.text = vo.Level > 0 ? vo.GetDesc() : "暂未学习";
            if (_lb_content_next != null) _lb_content_next.text = vo.IsMaxLevel ? "已满级" : vo.GetDesc(vo.Level + 1);

            if (_iconItem != null)
            {
                _iconItem.SetLevel(vo.Level.ToString());
                _iconItem.SetLocked(vo.Locked);
                _iconItem.SetSelected(true);
                _iconItem.SetIcon(vo.DisplayIcon);
            }
        }

        private void BindBtn(Image img, Action onClick)
        {
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
