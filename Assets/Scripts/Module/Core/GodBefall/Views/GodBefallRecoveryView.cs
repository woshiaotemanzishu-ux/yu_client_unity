using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临-神装回收/强化界面(对标老客户端 godBefall/GodBefallRecoveryView.ts):
    /// 顶部展示位(box_top 克隆 GodBefallRecoveryTopView,显当前降神系列强化等级)+ 标题(lable_title)+
    /// 降神系列页签横向列表(list_btn,克隆 GodBefallRecoveryBtnItem,点击切换系列、未满星禁切并提示)+
    /// 可回收装备纵向列表(list_equip,克隆 GodBefallEquipmentItem)+ 空提示(_gp_empty / _Image5 / _Label5)+
    /// 全选(img_btn_select)+ 一键强化(img_btn_streng,文本 lable_streng:"一键强化"/空时"前往获取")。
    ///
    /// 降级:GodBefallModel(GetClientGodConfig/SameQualityGodIsAllAwake/GetGodDataByConfig/GetStrongGodDic/
    /// GetGoodsByBagCond/LimitCanRecoveryEquip/select_equip_list/is_select 等)、config_god_star/ConfigGodBefall/
    /// config_god_stren 配置、强化协议(44018)、空时前往获取(OpenFun 27)、循环列表(LoopScrowViewMgr)、
    /// 顶部/页签/装备小项(_tpl_GodBefallRecoveryTopView/_tpl_GodBefallRecoveryBtnItem/_tpl_GodBefallRecoveryPropertyItem/
    /// _tpl_EquipmentItem/_tpl_GodBefallEquipmentItem)均未移植 →
    /// 模板先隐藏;按钮点击打日志「待对接」;空提示走默认显示;列表空、顶部/页签/装备走默认降级。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。无红点字段。
    /// </summary>
    public sealed class GodBefallRecoveryView : GodBefallRecoveryViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSucess/InitView:读 config_god_star/ConfigGodBefall → 铺页签(UpdateBtnItem)+ 顶部面板(UpdateTopPanel)+
            // 选默认系列(SameQualityGodIsAllAwake)+ 刷可回收装备列表(UpdateEquipItem)+ 空提示(SetEmptyTips)。数据未移植 → 空/默认。
            GameLog.Info("GodBefall", "GodBefallRecoveryView 打开 → 待对接 GodBefallModel/协议(列表空/默认降级)");
        }

        /// <summary>
        /// 各小项模板(由 TopView/BtnItem/PropertyItem/EquipmentItem 克隆铺设),数据/循环列表未移植先隐藏。
        /// </summary>
        private void HideTemplates()
        {
            // _tpl_* 是 GameObject 模板,直接 SetActive(false)(HideNode 收 Component,GameObject 不适用)。
            if (_tpl_GodBefallRecoveryBtnItem != null) _tpl_GodBefallRecoveryBtnItem.SetActive(false);
            if (_tpl_GodBefallRecoveryTopView != null) _tpl_GodBefallRecoveryTopView.SetActive(false);
            if (_tpl_GodBefallRecoveryPropertyItem != null) _tpl_GodBefallRecoveryPropertyItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_GodBefallEquipmentItem != null) _tpl_GodBefallEquipmentItem.SetActive(false);
        }

        private void BindButtons()
        {
            // 全选/取消全选(老端 img_btn_select:遍历可回收装备 AddInSelectList/DeleteInSelectList,上限 30)。
            BindBtn(img_btn_select, "全选/取消全选");
            // 一键强化(老端 img_btn_streng:满级提示「已满级」/ 列表空走前往获取(OpenFun 27)/ 未选提示 / 否则发协议 44018)。
            BindBtn(img_btn_streng, "一键强化 协议44018/前往获取");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/逻辑待对接)。</summary>
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
