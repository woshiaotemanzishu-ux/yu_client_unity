using Shenxiao.Generated.UI.Rune;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 符文技能子窗(对标老客户端 rune/RuneSkillView.ts):从九霄劫魄(符文)主面板按钮打开的子窗,标题「劫术天诀」。
    /// 9 个技能位(skillItemGp 克隆 _tpl_RuneSkillItem,按 posList 摆放)+ 选中技能后显技能名(skillName)/
    /// 解锁条件与描述(lockLb)+ 技能图标(skillImg)+ 可激活红点(activeRed)+ 关闭(_btn_close 关闭返回主面板)。
    ///
    /// 降级:RuneModel(rune_info.skill_lv、RuneEvent.SELECT_SKILL/SKILL_LV_UP)、SkillManager/SkillVo、
    /// config_rune_awake_skill、RuneSkillItem 列表项均未移植 → 不铺技能位、_tpl_RuneSkillItem 模板隐藏;
    /// activeRed 红点隐藏(老端按 CanSkillUp 显隐,降级先关);skillName/lockLb 保留预制体默认文案;
    /// _btn_close → Hide(老端 Close,关闭返回符文主面板)。
    /// </summary>
    public sealed class RuneSkillView : RuneSkillViewBind
    {
        protected override void OnInit()
        {
            // 红点降级:老端按 CanSkillUp 控制 activeRed 显隐,RuneModel 未移植 → 先隐藏。
            HideNode(activeRed);

            // 列表项模板隐藏:RuneSkillItem 未移植,不克隆铺位。
            if (_tpl_RuneSkillItem != null) _tpl_RuneSkillItem.SetActive(false);

            // 关闭按钮 → Hide(老端 _btn_close → Close,关闭返回符文主面板)。
            BindClose(_btn_close);
        }

        protected override void OnShow(object args)
        {
            // 老端 load_callback → InitView:铺 9 个技能位 + 默认选中。RuneModel/SkillManager 未移植 →
            // 不铺位,skillName/lockLb 保留预制体默认文案。
            GameLog.Info("Rune", "RuneSkillView 打开 → 待对接 RuneModel(列表空/默认降级)");
        }

        /// <summary>关闭按钮 → Hide(关闭返回符文主面板)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>动作按钮 → 日志降级(待对接 RuneModel 技能升级协议)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Rune", "点击[{0}] → 待对接", label));
        }

        /// <summary>隐藏节点(红点等)。</summary>
        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
