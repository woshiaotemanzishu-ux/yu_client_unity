using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临装备格(对标老客户端 godBefall/GodBefallEquipmentItem.ts):一个装备格,
    /// box 容器内克隆 EquipmentItem 显装备图标(type_id 图标 + 数量 num + 锁 lock),
    /// 支持多选(select_equip_list,上限 30):点击切换选中,SetSelect 显隐选中高亮。
    /// dataChanged 读 data[0]=vo / data[1]=custom_data(count/width/height/is_need_all_select)。
    ///
    /// 降级:GodBefallModel(select_equip_list/AddInSelectList/DeleteInSelectList/IS_SELECT_ALL_EQUIP 事件)、
    /// GoodsModel.GetMappingTypeId、EquipmentItem 子件(图标/选中图 select_image「bag_asset/select」)、
    /// ResManager 图集、SysInfo「选中数量已达上限」均未移植 →
    /// OnInit 隐藏 _tpl_* 模板;SetData 仅落最小渲染(数量文本 TODO,图标走 Model 待对接)、打日志「待对接」;
    /// SetSelect 仅占位。列表项,由神祇降临装备列表克隆/铺设。
    /// </summary>
    public sealed class GodBefallEquipmentItem : GodBefallEquipmentItemBind
    {
        protected override void OnInit()
        {
            // 子件模板(EquipmentItem / BaseAwardItem 克隆源),子件未移植 → 隐藏占位,避免直接显原型。
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            // 无红点 / 无按钮 —— 纯展示+选中项。
        }

        /// <summary>
        /// 填装备数据(对标 dataChanged → item.SetData(type_id, num, lock))。
        /// type_id 由 GoodsModel.GetMappingTypeId 映射、num=custom_data.count、lock=是否锁定。
        /// 降级:EquipmentItem 图标/锁 + GoodsModel 映射未移植 → 仅占位,图标走 Model 待对接。
        /// </summary>
        public void SetData(int typeId, int num, bool locked)
        {
            // 老端:克隆 EquipmentItem 显 type_id 图标 + 数量 num + 锁标记 lock,并居中布局到 box。
            // 子件/图集/映射未移植 → 不渲染图标,仅打日志。
            GameLog.Info("GodBefall",
                "GodBefallEquipmentItem.SetData(typeId={0}, num={1}, lock={2}) → 待对接 GodBefallModel/EquipmentItem 图标(GoodsModel.GetMappingTypeId)",
                typeId, num, locked);
        }

        /// <summary>设置选中高亮(对标 EquipmentItem.SetSelect)。选中图依赖 bag_asset/select(未移植)→ 仅占位。</summary>
        public void SetSelect(bool selected)
        {
            // 老端:item.select_image 显隐(ResManager 图集 bag_asset「select」)。子件未移植 → 占位。
            GameLog.Info("GodBefall", "GodBefallEquipmentItem.SetSelect({0}) → 待对接 EquipmentItem 选中高亮", selected);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
