using Shenxiao.Generated.UI.Pet;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Pet
{
    /// <summary>
    /// 御风云骑/外观主界面(对标老客户端 pet/OutWardBaseView.ts,HorseComponentView 等外观页复用此布局):3D 模型(res)+ 名/阶
    /// (res_name/res_stage)+ 星级(star_group/shadow_group)+ 战力(_gp_fight)+ 升级(lv_button/exp_group)+ 上一个/下一个外观
    /// (before_btn/after_btn)+ 属性(proptity_btn)/背包(bag_btn)/幻化(illusion_btn)/选择(select_btn)+ 顶部页签(btn_group_1/2)+ 进入(enter_btn)。
    ///
    /// 降级:OutwardModel/PetModel/config_outward/协议、子窗(属性/幻化/水晶/技能 PetProptityView/IllusionBaseView/PetCrystalView/PetSkillView)、
    /// 列表项(_tpl_*)均未移植 → 红点/模板隐藏、模型/星级/战力默认;各按钮打日志降级。无独立关闭按钮 → HUD 宠物图标再点关闭(PetFlow.Toggle)。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。其余宠物分页(剑魄同修/神巫/天妖灵魄)与本模块其它外观窗后续 tick 接。
    /// </summary>
    public sealed class OutWardBaseView : OutWardBaseViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → 读 OutwardModel 渲染模型 + 星级 + 战力 + 升级进度。数据未移植 → 默认降级。
            GameLog.Info("Pet", "御风云骑/外观打开 → 待对接 OutwardModel/PetModel(模型/星级/默认降级)");
        }

        private void HideReds()
        {
            HideNode(lv_btn_reddot); HideNode(bag_red); HideNode(illu_red);
            HideNode(btn_group_1_red); HideNode(btn_group_2_red);
        }

        private void HideTemplates()
        {
            if (_tpl_FairyWishEnterBtn != null) _tpl_FairyWishEnterBtn.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_PetRoundItem != null) _tpl_PetRoundItem.SetActive(false);
            if (_tpl_PetEquipOutItem != null) _tpl_PetEquipOutItem.SetActive(false);
        }

        private void BindButtons()
        {
            BindBtn(before_btn, "上一个外观");
            BindBtn(after_btn, "下一个外观");
            BindBtn(lv_button, "升级");
            BindBtn(select_btn, "使用/选择外观");
            BindBtn(proptity_btn, "属性 PetProptityView");
            BindBtn(bag_btn, "外观背包");
            BindBtn(illusion_btn, "幻化 IllusionBaseView");
            BindBtn(btn_switch, "切换");
            BindBtn(enter_btn, "进入");
            BindBtn(btn_group_1, "页签1");
            BindBtn(btn_group_2, "页签2");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子窗待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Pet", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
