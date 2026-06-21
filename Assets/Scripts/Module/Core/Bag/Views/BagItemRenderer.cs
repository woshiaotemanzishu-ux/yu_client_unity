using Shenxiao.Generated.UI.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包格子项(对标老客户端 bag/BagItemRenderer.ts):一个背包槽,内含克隆的 <see cref="BaseAwardItem"/>(真实物品图标 +
    /// 品质底板 + 数量),外加品质 grade/星级 star/锁 lock/限时 time_limit/装备升降箭头 up·down/禁用 ban/劣质 _bad_icon/红点 等覆盖件。
    ///
    /// 第 8 轮 P1:内联模板 _tpl_BaseAwardItem 是 common/BaseAwardItem.prefab 的嵌套实例(第 6 轮已回填 BaseAwardItem View 组件),
    /// 故克隆即得可用 View → SetData 走真实图标(对标 BaseAwardItem.SetData → GoodsModel.config_goods 图标/品质底板)。
    /// 列表项常被 <see cref="Views.BagComponentView"/> 克隆后直接 SetData(不经 BaseView.Show),OnInit 不会自动跑 →
    /// 用 <see cref="EnsureInit"/> 幂等保证模板克隆 + 覆盖件隐藏就位(不依赖 Show 时序)。
    /// 装备态覆盖件(grade/star/装备升降/锁/限时/红点)依赖装备配置(config_equip_attr 等,未移植)→ 本轮先隐藏,待装备系统补。
    /// </summary>
    public sealed class BagItemRenderer : BagItemRendererBind
    {
        private BaseAwardItem _item;
        private bool _inited;

        protected override void OnInit()
        {
            EnsureInit();
        }

        /// <summary>幂等初始化:克隆 BaseAwardItem 模板进 conta(对标 load_callback:new BaseAwardItem(this.conta))+ 隐覆盖件。</summary>
        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            if (_tpl_BaseAwardItem != null && conta != null)
            {
                GameObject go = Instantiate(_tpl_BaseAwardItem, conta);
                go.SetActive(true);
                _item = go.GetComponent<BaseAwardItem>();
            }
            HideOverlays();
        }

        /// <summary>填格子数据(对标 dataChanged);null/Count&lt;=0 = 空槽。有物品则走 BaseAwardItem 真实图标 + 品质底板 + 数量。</summary>
        public void SetData(BagItemData data)
        {
            EnsureInit();
            bool hasItem = data != null && data.Count > 0;
            ShowEmpty(!hasItem);

            // 真实图标 + 品质底板 + 数量(对标 BaseAwardItem.SetData → GoodsModel.GetGoodsBasicByTypeId → goods_icon/color)。
            if (hasItem && _item != null)
            {
                _item.SetData(data.TypeId, data.Count);
                // 点击带 BagGoods 实例 → 装备实例 tips(极品 equip_extra_attr / 强化 stren);无实例(完成弹层等)走 BaseAwardItem 默认 Show(typeId,num)。
                if (data.Goods != null) _item.SetClickCallBack(() => ItemTipsView.Show(data.Goods));
            }

            HideOverlays();  // 品质角标/星级/装备升降/锁/限时/红点 依赖装备配置(未移植),本轮先隐藏
        }

        private void ShowEmpty(bool empty)
        {
            if (defaultImg != null) defaultImg.gameObject.SetActive(empty);
            if (_item != null) _item.gameObject.SetActive(!empty);
        }

        private void HideOverlays()
        {
            if (up != null) up.gameObject.SetActive(false);
            if (down != null) down.gameObject.SetActive(false);
            if (ban != null) ban.gameObject.SetActive(false);
            if (grade != null) grade.text = "";
            if (star_group != null) star_group.gameObject.SetActive(false);
            if (@lock != null) @lock.gameObject.SetActive(false);
            if (time_limit != null) time_limit.gameObject.SetActive(false);
            if (redMask != null) redMask.gameObject.SetActive(false);
            if (_bad_icon != null) _bad_icon.gameObject.SetActive(false);
        }
    }

    /// <summary>背包格子数据(对标 BagGoods 的显示字段:type_id → 真实图标/品质,goods_num → 数量)。
    /// <see cref="Goods"/> 携真实 <see cref="BagGoods"/> 实例(装备 tips 极品/强化实例属性透传;非背包来源可空 → 走 typeId 默认 tips)。</summary>
    public sealed class BagItemData
    {
        public int TypeId;
        public long Count;
        public BagGoods Goods;
    }
}
