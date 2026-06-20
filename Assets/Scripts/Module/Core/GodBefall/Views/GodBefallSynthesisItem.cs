using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临-合成项(对标老客户端 godBefall/GodBefallSynthesisItem.ts):一行合成配方 ——
    /// 左侧材料装备列表(list_equip 克隆 GodBefallEquipmentItem 铺 data.equip_arry)+ 等号图(img_equal)+
    /// 右侧产物列表(list_single_equip 铺 data.show_goods)+ 勾选框(img_check_bg 点击切换 / img_check_select 选中标记)。
    /// 勾选 → AddCompositionSelectList(data) 入选,取消 → DeleteInCompositionSelectList(data);默认选中。
    ///
    /// 降级:GodBefallModel(select_composition_list / AddCompositionSelectList / SetPosAndState)、
    /// GodBefallEquipmentItem 子件、LoopScrowViewMgr 横向循环列表(list_equip / list_single_equip)均未移植 →
    /// OnInit 仅挂勾选框点击(本地切换 img_check_select 显隐 + 打日志);SetData 仅落选中态、列表/图标待对接打日志;
    /// 无红点、无 _tpl_* 模板。列表项,由合成面板克隆/铺设。
    /// </summary>
    public sealed class GodBefallSynthesisItem : GodBefallSynthesisItemBind
    {
        /// <summary>本行是否被勾选(对标 img_check_select.visible);默认选中。</summary>
        private bool _selected = true;

        protected override void OnInit()
        {
            // 无红点 / 无 _tpl_* 模板 —— 仅勾选框需绑定。
            // 默认选中(对标 load_callback:img_check_select.visible = true)。
            if (img_check_select != null) img_check_select.gameObject.SetActive(true);

            // 勾选框背景:对标 InitEvent → 点击切换选中,入选/取消选 GodBefallModel(降级:本地切换 + 打日志)。
            BindBtn(img_check_bg, "切换合成勾选 AddCompositionSelectList/DeleteInCompositionSelectList");
        }

        /// <summary>
        /// 填合成行(对标 dataChanged → UpdateEquipMentItem / UpdateSingleEquipMentItem / AddDataFunc)。
        /// 老端:list_equip 铺 data.equip_arry、list_single_equip 铺 data.show_goods(均经 SetPosAndState 算位/态),
        /// 并按勾选态入选 select_composition_list。降级:Model/子件/循环列表未移植 → 仅落选中态、打日志「待对接」。
        /// </summary>
        public void SetData(object data)
        {
            // 默认勾选(老端 load_callback 默认选中,dataChanged 末尾按当前勾选态 AddDataFunc)。
            SetSelect(_selected);
            GameLog.Info("GodBefall", "GodBefallSynthesisItem.SetData → 待对接 GodBefallModel(SetPosAndState/select_composition_list)+ GodBefallEquipmentItem 材料/产物列表");
        }

        /// <summary>设置勾选态(对标 img_check_select.visible + AddDataFunc → SelectFunc 入选/取消)。</summary>
        public void SetSelect(bool selected)
        {
            _selected = selected;
            if (img_check_select != null) img_check_select.gameObject.SetActive(selected);
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
