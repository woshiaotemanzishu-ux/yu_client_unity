using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临-装备合成界面(对标老客户端 godBefall/GodBefallSynthesisView.ts):
    /// 标题(lable_title)+ 底框(img_bg/img_bottom_bg)+ 可合成装备列表(list_equip,LoopScrowViewMgr 铺 GodBefallSynthesisItem)+
    /// 空列表提示(empty_gp)+ 合成按钮(img_btn_synthesis「合成」,选中目标→发 44016 协议)+ 关闭(img_close_btn)+ 红点(img_red)。
    ///
    /// 降级:GodBefallModel(all_composition_list/select_composition_list/is_open_synthesis_view)、CompositeModel、
    /// 合成规则配置(compManu_cfg/comp_equip_rule_cfg)、ErlangParser、LoopScrowViewMgr 循环列表、44016 合成协议、
    /// SysInfo 提示(没有可合成装备/未选择合成目标)均未移植 →
    /// 红点(img_red)/模板(_tpl_GodBefallSynthesisItem)先隐藏;列表走空、显空提示(empty_gp);
    /// 按钮点击仅打日志「待对接」。事件驱动窗口,数据/协议待对接。
    /// </summary>
    public sealed class GodBefallSynthesisView : GodBefallSynthesisViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess/InitView:读 GodBefallModel.all_composition_list 排序铺列表 + 按是否有数据切红点/空提示。
            // 数据层未移植 → 列表空、显空提示(empty_gp)、隐红点。
            if (empty_gp != null) empty_gp.gameObject.SetActive(true);
            HideNode(img_red);
            GameLog.Info("GodBefall", "GodBefallSynthesisView 打开 → 待对接 GodBefallModel/协议(列表空/默认降级)");
        }

        /// <summary>红点(img_red)依赖 GodBefallModel.all_composition_list 是否非空,未移植先隐藏。</summary>
        private void HideReds()
        {
            HideNode(img_red);
        }

        /// <summary>合成装备小项模板(由 GodBefallSynthesisItem 克隆),数据未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_GodBefallSynthesisItem != null) _tpl_GodBefallSynthesisItem.SetActive(false);
        }

        private void BindButtons()
        {
            BindBtn(img_close_btn, "关闭");
            BindBtn(img_btn_synthesis, "合成 → 44016 协议");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:逻辑/协议待对接)。</summary>
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
