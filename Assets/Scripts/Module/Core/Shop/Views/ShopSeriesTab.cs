using System;
using Shenxiao.Generated.UI.Shop;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using UnityEngine;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 商城二级子页签行(对标老端 shop/ShopSeriesTab.ts):文案(labelDisplay,ClientShopConfig.ShopSeries 的
    /// desc)+ 选中态换图(uiscv2_007 选中/uiscv2_008 未选中,颜色随选中态切白/紫)。由 ShopCommonView 按
    /// <see cref="Shenxiao.Module.Core.Shop.ShopConfigs.GetShopSeries"/> 过滤后克隆填充(仅灵玉/善缘两类型有子页签)。
    /// 降级:结社(Guild)专属红点(redDisplay,老端按 RedDotController RedKey.GUILD_SHOP1/2)未接线,常隐藏(TODO)。
    /// </summary>
    public sealed class ShopSeriesTab : ShopSeriesTabBind
    {
        private static readonly Color SelectedText = Color.white;
        private static readonly Color UnselectedText = new Color(0.31f, 0.325f, 0.56f); // #4f538f

        public int SeriesId { get; private set; }
        private Action _onClick;

        protected override void OnInit()
        {
            if (redDisplay != null) redDisplay.gameObject.SetActive(false); // 结社专属红点未接线,TODO
            if (_Image1 != null)
            {
                _Image1.raycastTarget = true;
                UIUtil.AddClick(_Image1, OnClick);
            }
        }

        /// <summary>填一条系列子页签(对标 ShopSeriesTab.dataChanged)。</summary>
        public void SetData(int seriesId, string desc, bool selected, Action onClick)
        {
            SeriesId = seriesId;
            _onClick = onClick;
            if (labelDisplay != null) labelDisplay.text = desc;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (labelDisplay != null) labelDisplay.color = selected ? SelectedText : UnselectedText;
            if (_Image1 != null)
            {
                string key = GameResPath.GetIcon("shop", selected ? "uiscv2_007" : "uiscv2_008");
                _ = ResManager.SetImageAsync(_Image1, key, false, false);
            }
        }

        private void OnClick() => _onClick?.Invoke();
    }
}
