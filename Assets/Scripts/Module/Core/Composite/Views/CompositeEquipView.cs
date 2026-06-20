using Shenxiao.Generated.UI.Composite;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Composite
{
    /// <summary>
    /// 装备合成(对标老客户端 composite/CompositeEquipView.ts):右侧分类页签(rBtn_0..7 切合成类别)+ 合成区(mat_group/mat_pos_0..4 放材料,
    /// _tpl_EquipmentItem 克隆)+ 一键添加(btnOneKeyAdd)/合成(btnComposite)+ 选装备(equipBtn1/2)+ 规则(ruleBtn)/排行(rankBtn→CompositeRankView)+
    /// 下拉(dropListCon,_tpl_DownDropBtn)+ 各红点(rBtn_red*/one_key_add_red/composite_red/reuipRed*)+ 失败标(com_fail)。
    ///
    /// 降级:CompositeModel/GoodsModel/config_compound/合成协议、_tpl_*(DownDropBtn/EquipmentItem/CompositeEquipResolveView/UIVerTabBar)、
    /// 各分类数据均未移植 → 红点/失败标/模板隐藏、材料位空;分类页签与一键添加/合成/选装备/规则按钮打日志;排行经 CompositeFlow.OpenSub
    /// 前向接(子窗未写时日志)。无独立关闭按钮 → HUD 合成图标再点关闭(CompositeFlow.Toggle)。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class CompositeEquipView : CompositeEquipViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → 读 config_compound 铺分类页签 + 选默认类 + 刷材料/可合成装备。数据未移植 → 空/默认降级。
            GameLog.Info("Composite", "装备合成打开 → 待对接 CompositeModel(分类/材料空/默认降级)");
        }

        private void HideReds()
        {
            HideNode(rBtn_red0); HideNode(rBtn_red1); HideNode(rBtn_red2); HideNode(rBtn_red3);
            HideNode(rBtn_red4); HideNode(rBtn_red5); HideNode(rBtn_red6); HideNode(rBtn_red7);
            HideNode(one_key_add_red); HideNode(composite_red);
            HideNode(reuipRed1); HideNode(reuipRed2); HideNode(com_fail);
        }

        private void HideTemplates()
        {
            if (_tpl_DownDropBtn != null) _tpl_DownDropBtn.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_CompositeEquipResolveView != null) _tpl_CompositeEquipResolveView.SetActive(false);
            if (_tpl_UIVerTabBar != null) _tpl_UIVerTabBar.SetActive(false);
        }

        private void BindButtons()
        {
            // 右侧合成分类页签(数据驱动哪些显示/文案,待 config_compound;先绑日志)。
            BindBtn(rBtn_0, "合成分类0"); BindBtn(rBtn_1, "合成分类1"); BindBtn(rBtn_2, "合成分类2"); BindBtn(rBtn_3, "合成分类3");
            BindBtn(rBtn_4, "合成分类4"); BindBtn(rBtn_5, "合成分类5"); BindBtn(rBtn_6, "合成分类6"); BindBtn(rBtn_7, "合成分类7");
            // 操作按钮。
            BindBtn(btnOneKeyAdd, "一键添加材料");
            BindBtn(btnComposite, "合成");
            BindBtn(equipBtn1, "选装备1");
            BindBtn(equipBtn2, "选装备2");
            BindBtn(ruleBtn, "合成规则");
            // 排行子窗(已生成 Bind,View 待写)→ 前向 OpenSub。
            BindOpen(rankBtn, "CompositeRankView", "合成排行");
        }

        /// <summary>按钮 → 打开合成模块内子窗(CompositeFlow.OpenSub 按 View 子类名查找并叠在主面板上)。</summary>
        private void BindOpen(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Composite", "点击[{0}] → 打开 {1}", label, viewType);
                CompositeFlow.OpenSub(viewType);
            });
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/逻辑待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Composite", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
