using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Guild;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 仓库/捐献物品格子(对标老客户端 guild/GuildDepotItem.ts):复用于两处——主仓库列表(<see cref="GuildDepotView"/>,
    /// 点击=兑换)与捐献选择弹层(<see cref="GuildDepotSelectView"/>,点击=切换选中)。降级:tip_up/down/compesite/ban
    /// (装备对比/合成提示)/装备 tips 弹窗——本轮均隐藏不接,仅保留图标(<see cref="EquipmentItem"/>)+数量+选中态+点击回调。
    /// </summary>
    public sealed class GuildDepotItem : GuildDepotItemBind
    {
        private EquipmentItem _icon;
        private System.Action _onClick;

        protected override void OnInit()
        {
            if (tip_up != null) tip_up.gameObject.SetActive(false);
            if (tip_down != null) tip_down.gameObject.SetActive(false);
            if (tip_compesite != null) tip_compesite.gameObject.SetActive(false);
            if (tip_ban != null) tip_ban.gameObject.SetActive(false);
            if (selectBg != null) selectBg.gameObject.SetActive(false);
            BindClick(_box_click, () => _onClick?.Invoke());
        }

        public void SetData(int typeId, long num, bool selected, System.Action onClick)
        {
            _onClick = onClick;
            EnsureIcon();
            if (_icon != null) _icon.SetData(typeId, num);
            if (selectBg != null) selectBg.gameObject.SetActive(selected);
        }

        private void EnsureIcon()
        {
            if (_icon != null || _tpl_EquipmentItem == null || _group_item == null) return;
            GameObject go = Object.Instantiate(_tpl_EquipmentItem, _group_item);
            go.SetActive(true);
            go.name = "EquipmentItem";
            _icon = go.GetComponent<EquipmentItem>();
        }

        private static void BindClick(UnityEngine.Component target, System.Action onClick)
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
