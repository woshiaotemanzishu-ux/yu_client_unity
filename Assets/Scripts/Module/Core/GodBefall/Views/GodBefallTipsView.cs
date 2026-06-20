using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临提示窗(对标老客户端 godBefall/GodBefallTipsView.ts):降临/激活/升星/觉醒三态共用的弹窗 ——
    /// 顶部标题(_img_title 按 ViewType 切 ui_js_33/34/35)+ 3D 模型展示位(_box_model)+ 多层背景(_img_bg/_img_bg2/_img_bg3)。
    /// 三态盒子互斥:激活(_box_active:战力 _box_fight、属性 _panel_stren_attr/_html_stren_attr(2)、技能 _box_active_skills)、
    /// 升星(_box_star:当前/下一星图 _img_cur_star/_img_next_star、属性 _panel_star_attr/_html_star_attr(2)、天赋 _box_star_talent)、
    /// 觉醒(_box_awaken:_img_awaken_cur/_lv/_lv2/_new、属性 _panel_awaken_attr/_html_awaken_attr(2))。
    /// 底部按钮区(_box_btns):使用(_img_use/_lb_use → 协议 44006 后关闭)、确定(_img_ok/_lb_ok → 关闭,带倒计时文案)。
    ///
    /// 降级:GodBefallModel(GetGodData/GetStageData/GetStarData/GetTalentList/GetSkillList、ACTIVE_TIPS_CLOSE 事件)、
    /// config_god_lv/star/stage 配置、协议 44006、3D 模型(ResManager.SetRoleModel)、属性文案(WordManager)、
    /// 战力/技能小项(_tpl_FightingShowSmallItem/_tpl_GodBefallSkillItem)、入场 Tween 动画与倒计时关闭(GodBefallTweenValue)均未移植 →
    /// 模板先隐藏;按钮点击打日志「待对接」;三态盒子/属性/模型走默认。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class GodBefallTipsView : GodBefallTipsViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess + UpdateView:按 scmd(grade/star/激活)切三态盒子 + 铺 3D 模型 + 属性/天赋/技能 + 入场动画 + 倒计时。
            // GodBefallModel/配置/协议/模型/Tween 均未移植 → 列表空、属性默认降级。
            GameLog.Info("GodBefall", "GodBefallTipsView 打开 → 待对接 GodBefallModel/协议(列表空/默认降级)");
        }

        /// <summary>无红点字段 —— 该窗为纯展示弹窗,无红点占位。</summary>
        private void HideReds()
        {
            // Bind 中无 red/reddot 字段,留空。
        }

        /// <summary>战力/技能小项模板(由 FightingShowSmallItem/GodBefallSkillItem 克隆),数据未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_GodBefallSkillItem != null) _tpl_GodBefallSkillItem.SetActive(false);
        }

        private void BindButtons()
        {
            // 老端 InitEvent:_img_use → Fire(REQUEST_PROTO, 44006, ...) 后 Close;_img_ok → Close。
            BindBtn(_img_use, "使用 协议44006");
            BindBtn(_img_ok, "确定/关闭");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/关闭待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("GodBefall", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
