using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.Role;
using Shenxiao.Module.Core.Skill;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 被动技能项(对标老客户端 role/SkillPassiveItem.ts):图标(_img_icon)+ 名(_lb_name)+ 等级(_lb_lv)+ 选中(_img_select)/红点(_img_red),
    /// 内含子项模板(_tpl_SkillPassiveSubItem)。
    ///
    /// **RoleFlow 技能"被动技能"tab 容器**(技能成长线轮3 接入):这是 RoleModule.prefab 顶层唯一捕获的被动技能节点
    /// (148×173,老端本是滚动列表里的一枚"图标行",老端真正的整页容器 SkillPassiveSubItem——含 _Scroller1 列表 +
    /// 描述/升级面板——在此快照里变成它的子节点,而非反过来;_Scroller1.content 无子项也无 LayoutGroup,
    /// 没有可用的多技能列表基建)。按"布局归 prefab"约束不臆造坐标:本轮把这对(自身图标 + 内嵌 SkillPassiveSubItem
    /// 详情面板)当一整块"当前被动技能"单槽展示使用,只显 <see cref="SkillConfigs.IsPassive"/> 过滤后 id 最小的一个。
    /// 多被动技能滚动切换需要美术/UiCreator 补 _Scroller1 内容 Layout,留 TODO(规格 §3 汇报的简化项)。
    /// </summary>
    public sealed class SkillPassiveItem : SkillPassiveItemBind
    {
        private SkillPassiveSubItem _detail;

        protected override void OnInit()
        {
            if (_img_select != null) _img_select.gameObject.SetActive(false);
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            if (_tpl_SkillPassiveSubItem != null)
            {
                _tpl_SkillPassiveSubItem.SetActive(true); // 原地复用为详情面板(见类头注释),非模板克隆
                _detail = _tpl_SkillPassiveSubItem.GetComponent<SkillPassiveSubItem>();
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

        public void SetData(string name, string lv)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
            if (_lb_lv != null) _lb_lv.text = lv ?? "";
        }

        public void SetSelected(bool sel)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(sel);
        }

        /// <summary>取真实被动技能列表里 id 最小的一项展示(对标 SkillManager.GetShowPassitiveSkillList 的过滤意图;
        /// 老端用 ConfigSkillUI.passitiveSkillList 表过滤,但该表在当前客户端配置里已不存在/为空,改走
        /// config_skill.type==2 真实字段过滤,见 SkillConfigs.IsPassive 注释)。</summary>
        private void Refresh()
        {
            SkillVo best = null;
            foreach (SkillVo vo in SkillManager.Instance.AllSkills)
            {
                if (!SkillConfigs.IsPassive(vo.Id)) continue;
                if (best == null || vo.Id < best.Id) best = vo;
            }

            SetData(best?.GetName() ?? "", best != null ? best.Level.ToString() : "");
            SetSelected(best != null);
            if (_img_icon != null)
            {
                _img_icon.enabled = best != null;
                if (best != null) _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetSkillIcon(best.DisplayIcon), nativeSize: false);
            }
            _detail?.SetData(best);
        }
    }
}
