using Shenxiao.Generated.UI.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 物品信息项(对标老客户端 common/ItemInfoItem.ts):列表项渲染器,iconBox 内放一个装备件(EquipmentItem),
    /// 数据来时把 reward 映射成 type_id 后 SetData 给内部装备件(Laya 里尺寸 87px)。
    ///
    /// 范本:克隆 _tpl_EquipmentItem 进 iconBox(对标 MainUIDownView 模板克隆)。降级:GoodsModel 映射
    /// (reward→type_id)未移植 → SetData 直接收 typeId;内部 EquipmentItem 自身已降级(覆盖层隐藏、图标待接)。
    /// </summary>
    public sealed class ItemInfoItem : ItemInfoItemBind
    {
        /// <summary>内部装备件像素尺寸(对标 Laya SetItemSize(87,87);基准 127px → 缩放)。</summary>
        [SerializeField] private float _itemSize = 87f;

        private EquipmentItem _item;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipmentItem != null && iconBox != null)
            {
                GameObject go = Instantiate(_tpl_EquipmentItem, iconBox);
                go.SetActive(true);
                _item = go.GetComponent<EquipmentItem>();
                if (_item != null) _item.SetScale(_itemSize / 127f);
            }
        }

        /// <summary>填物品(对标 dataChanged → _item.SetData)。type_id 映射待 GoodsModel,先直收 typeId。</summary>
        public void SetData(int typeId, long num, bool isLock = false)
        {
            if (_item != null) _item.SetData(typeId, num, isLock);
        }
    }
}
