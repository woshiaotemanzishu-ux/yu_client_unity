using Shenxiao.Generated.UI.HolySeal;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 影骸战衣页(对标老客户端 holySeal/HolySealView.ts,BagView 标签2「影骸战衣」内容):战衣装备位(_gp_equips)+ 背包列表(_list_bag)+
    /// 战力(_lb_fighting)+ 强化(_gp_stren)/预览(_gp_preview)/分解(_gp_decompose)/合成(_gp_composite)/魂(_gp_soul)入口 + 各红点。
    ///
    /// 降级:HolySealModel/影骸战衣协议、HolySealEquipItem 列表项均未移植 → 5 红点/模板隐藏、列表空、战力默认;5 个功能按钮(强化/预览/分解/合成/魂)+ _btn_holy 打日志降级。
    /// 无独立关闭按钮(背包窗框统一关闭)→ 作 BagView 标签2 内容由 BagFlow 跨模块 reparent 进窗框内容区(shared-prefab 独立 HolySealView.prefab,根即视图)。事件驱动,不进 FirstPass。
    /// </summary>
    public sealed class HolySealView : HolySealViewBind
    {
        protected override void OnInit()
        {
            HideNode(_img_stren_red);
            HideNode(_img_preview_red);
            HideNode(_img_decompose_red);
            HideNode(_img_composite_red);
            HideNode(_img_soul_red);
            if (_tpl_HolySealEquipItem != null) _tpl_HolySealEquipItem.SetActive(false);
            BindBtn(_btn_holy, "影骸战衣·主操作");
            BindBtn(_img_stren, "影骸战衣·强化");
            BindBtn(_img_preview, "影骸战衣·预览");
            BindBtn(_img_decompose, "影骸战衣·分解");
            BindBtn(_img_composite, "影骸战衣·合成");
            BindBtn(_img_soul, "影骸战衣·魂");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 HolySealModel 铺战衣位/背包/战力。数据未移植 → 默认降级。
            GameLog.Info("Bag", "影骸战衣页打开 → 待对接 HolySealModel(默认降级)");
        }

        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Bag", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
