using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临-神契装备界面(对标老客户端 godBefall/GodBefallEquipView.ts):
    /// 顶部模型展示位(_box_model 走 ResManager.SetRoleModel 渲染神祇模型)+ 名称(_lb_name)+ 战力小项(_fsb_item 克隆 FightingShowSmallItem)+
    /// 星级(_gp_star)+ 四部位装备格(_box_equip 克隆 GodBefallEquipItem,按 equip_item_pos_dic 落位)+
    /// 神匠(_btn_com「合成」→ CompositeModel.OPEN_COMPOSITE_VIEW(God) 并关窗)+
    /// 一键穿戴/前往获取(_btn_wear,文案随可换装数量切「一键穿戴」/「前往获取」)+ 说明(_img_help → OPEN_INSTRUCTION_VIEW 4401)+
    /// 关闭(_img_close)+ 品质/类型下拉(color_con/type_con 克隆 DownDropBtn)+ 神契背包列表(_Scroller1 + GodBefallBagItem 循环列表)+
    /// 升星提示(_img_tips/gp_tips/_lb_tips)+ 红点(_red_com 合成红点 / _img_wear_red 穿戴红点)。
    ///
    /// 降级:GodBefallModel(GetGodVoInDic/GetEquipList/GetGoodsByBagCond/CheckEquipCanReplaceOne/GetCurCanSetEquip2 等)、
    /// GoodsModel/CompositeModel、config_god_star/config_god_star_up_limit/ConfigGodBefall 配置、
    /// 穿戴/合成协议(44012)、ResManager 神祇模型、DownDropBtn 下拉、FightingShowSmallItem 战力、
    /// GodBefallBagItem/GodBefallEquipItem 列表项与各 GodBefallEvent 均未移植 →
    /// 红点先隐藏;_tpl_* 模板隐藏;按钮点击打日志「待对接」;OnShow 列表空/模型默认降级。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。Null-guard 每处访问。
    /// </summary>
    public sealed class GodBefallEquipView : GodBefallEquipViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 Open/open_callback → UpdateView:渲染神祇模型 + 铺四部位装备 + 刷背包列表/战力/名称/穿戴态。
            // GodBefallModel/协议/配置/模型均未移植 → 列表空、模型/默认降级。
            GameLog.Info("GodBefall", "GodBefallEquipView 打开 → 待对接 GodBefallModel/协议(列表空/默认降级)");
        }

        /// <summary>红点:合成红点 _red_com(GetAutoComposeRed)、穿戴红点 _img_wear_red(可换装数量),数据未移植先隐藏。</summary>
        private void HideReds()
        {
            HideNode(_red_com);
            HideNode(_img_wear_red);
        }

        /// <summary>列表项模板(背包格/下拉按钮/战力小项/装备格),由各 Item View 克隆,数据未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_GodBefallBagItem != null) _tpl_GodBefallBagItem.SetActive(false);
            if (_tpl_DownDropBtn != null) _tpl_DownDropBtn.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_GodBefallEquipItem != null) _tpl_GodBefallEquipItem.SetActive(false);
        }

        private void BindButtons()
        {
            BindBtn(_img_close, "关闭");
            BindBtn(_btn_com, "神匠合成 CompositeModel.OPEN_COMPOSITE_VIEW(God)");
            BindBtn(_btn_wear, "一键穿戴/前往获取 协议44012");
            BindBtn(_img_help, "说明 OPEN_INSTRUCTION_VIEW(4401)");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子窗待对接)。</summary>
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
