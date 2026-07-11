using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Jewel;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 雕刻子窗左侧装备位选择行(对标老客户端 jewel/EquipJewelCraveSubItem.ts):部位名(name_label)+ 雕刻等级
    /// (level_label)+ 选中框(select_img)+ 未解锁遮罩(black_mask/lock_label)+ 六格镶嵌预览(jewel_icon_group/
    /// _img_1.._img_6)+ 合成红点(red_dot)。点击 click_group → 回调父面板切换选中装备位
    /// (对标 SELECT_JEWEL_CARVE_GOODS)。
    ///
    /// 由 <see cref="EquipJewelCraveView"/> 按当前穿戴装备位(1..10,<see cref="EquipAutoWear.GetWorn"/>)克隆
    /// <c>_tpl_EquipJewelCraveSubItem</c> 铺入 Content11。
    ///
    /// 降级:未解锁遮罩(config_equip_stone_pos_unlock 门槛)/六格镶嵌预览(依赖 GoodsDynamicModel 详情异步到位)/
    /// 合成红点(config_equip_stone_lv)均未移植数据源 → 一律隐藏,不臆造;部位名(GoodsModel 真配置)与雕刻等级
    /// (EquipJewelModel 真数据,15210 落库后)为真实展示。
    /// </summary>
    public sealed class EquipJewelCraveSubItem : EquipJewelCraveSubItemBind
    {
        public int EquipType { get; private set; }

        private System.Action<int> _onClick;

        protected override void OnInit()
        {
            HideNode(red_dot);
            HideNode(select_img);
            HideNode(black_mask);
            HideNode(lock_label);   // 未解锁提示:本页签只铺"已穿戴"装备位,天然可雕刻,无需锁定文案
            HideJewelIcons();

            BindClick(click_group, () => _onClick?.Invoke(EquipType));
        }

        /// <summary>设置选中回调(由 <see cref="EquipJewelCraveView"/> 铺格时挂)。</summary>
        public void SetClickCallback(System.Action<int> onClick) => _onClick = onClick;

        /// <summary>填该装备位一行(对标 SetData):部位名(GoodsModel 真配置)+ 雕刻等级
        /// (EquipJewelModel.GetCrave,未查询到显 0)。</summary>
        public void SetData(int equipType)
        {
            EquipType = equipType;
            if (name_label != null) name_label.text = GoodsModel.GetEquipPosName(equipType);
            int refineLv = EquipJewelModel.Instance.GetCrave(equipType)?.RefineLv ?? 0;
            if (level_label != null) level_label.text = "Lv." + refineLv;
        }

        public void SetSelected(bool selected)
        {
            if (select_img != null) select_img.gameObject.SetActive(selected);
        }

        private void HideJewelIcons()
        {
            HideNode(_img_1);
            HideNode(_img_2);
            HideNode(_img_3);
            HideNode(_img_4);
            HideNode(_img_5);
            HideNode(_img_6);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        private void BindClick(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
