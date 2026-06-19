using Shenxiao.Generated.UI.Bag;
using Shenxiao.Generated.UI.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包格子项(对标老客户端 bag/BagItemRenderer.ts):一个背包槽,内含克隆的 BaseAwardItem(物品图标+数量),
    /// 外加品质 grade/星级 star/锁 lock/限时 time_limit/装备升降箭头 up·down/禁用 ban/劣质 _bad_icon/红点 等覆盖件。
    ///
    /// 降级:背包数据层(GoodsModel/BagModel/EquipModel/config_goods/config_equip_attr)未移植,
    /// BaseAwardItem 也只有 Bind(无 View)→ OnInit 克隆 BaseAwardItem 模板进 conta、隐藏全部覆盖件、默认显
    /// defaultImg(空槽);SetData(BagItemData) 有物品则显数量、无则空槽。物品图标/品质/星级/装备态/红点等
    /// 依赖物品配置,先隐藏/不设,待背包协议 + 配置移植后补。列表项,由 BagComponentView 克隆。
    /// </summary>
    public sealed class BagItemRenderer : BagItemRendererBind
    {
        private BaseAwardItemBind _item;

        protected override void OnInit()
        {
            // 对标 load_callback:new BaseAwardItem(this.conta) —— 克隆 BaseAwardItem 模板进 conta。
            if (_tpl_BaseAwardItem != null && conta != null)
            {
                GameObject go = Instantiate(_tpl_BaseAwardItem, conta);
                go.SetActive(true);
                _item = go.GetComponent<BaseAwardItemBind>();
            }
            HideOverlays();
            ShowEmpty(true);
        }

        /// <summary>填格子数据(对标 dataChanged);null/Count&lt;=0 = 空槽。</summary>
        public void SetData(BagItemData data)
        {
            bool hasItem = data != null && data.Count > 0;
            ShowEmpty(!hasItem);

            if (hasItem && _item != null && _item.num_text != null)
            {
                // 叠加数量 >1 才显(对标 BaseAwardItem 数量显示)。物品图标依赖 goods 配置/图集路径(未移植),暂不设。
                _item.num_text.text = data.Count > 1 ? data.Count.ToString() : "";
            }

            HideOverlays();  // 品质/星级/装备态/锁/限时/红点 依赖配置,未移植先隐藏
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

    /// <summary>背包格子最小数据(待 GoodsModel/BagModel + 物品配置移植后填:图标/数量/品质/星级…)。</summary>
    public sealed class BagItemData
    {
        public int Count;
        public string IconPath;  // 预留:物品图标(待 goods 配置)
    }
}
