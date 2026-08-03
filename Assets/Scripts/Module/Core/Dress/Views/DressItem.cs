using System;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Dress;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Dress
{
    public sealed class DressItem : DressItemBind
    {
        private Action _onClick;
        private DressConfigs.Row _row;
        private bool _selected;

        public uint DressId => _row?.Id ?? 0;
        public byte DressType => _row?.Type ?? 0;
        public Graphic ClickSurface => bg;

        protected override void OnInit()
        {
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            if (ClickSurface != null) UIUtil.AddClick(ClickSurface, () => _onClick?.Invoke());
        }

        public void SetData(DressConfigs.Row row, bool selected, DressModel.Entry entry, bool worn, Action onClick)
        {
            _row = row;
            _onClick = onClick;
            if (dress_name != null) dress_name.text = row?.Name ?? "";
            if (gray_bg != null) gray_bg.gameObject.SetActive(row == null || row.Id == 0);
            if (bg != null) bg.gameObject.SetActive(row != null && row.Id != 0);
            if (use_tag != null) use_tag.gameObject.SetActive(worn);
            if (red_dot != null) red_dot.gameObject.SetActive(false);

            int turn = DressConfigs.GetTurnCondition(row);
            if (fight != null)
            {
                if (worn) fight.gameObject.SetActive(false);
                else
                {
                    fight.gameObject.SetActive(true);
                    fight.text = entry != null ? "Lv." + entry.DressLevel : (turn > 0 ? turn + "转可激活" : "未激活");
                    fight.color = entry != null ? new Color32(10, 149, 62, 255) : new Color32(254, 26, 26, 255);
                }
            }
            if (_lb_fight != null) _lb_fight.gameObject.SetActive(false);
            SetSelected(selected);
            LoadIcon(row);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (select_bg != null) select_bg.gameObject.SetActive(selected);
        }

        private async void LoadIcon(DressConfigs.Row row)
        {
            if (head == null || row == null) return;
            string path = "";
            if (row.Type == DressView.HeadType)
            {
                int career = RoleModel.Instance.Career > 0 ? RoleModel.Instance.Career : 1;
                string icon = DressConfigs.GetHeadIcon(row, career);
                if (!string.IsNullOrEmpty(icon)) path = GameResPath.GetHeadPath(icon);
            }
            else
            {
                DressConfigs.CostValue cost = DressConfigs.GetFirstCost(row);
                if (cost != null)
                {
                    var mapped = GoodsModel.GetMappingTypeId(cost.Type, cost.TypeId);
                    int goodsId = mapped.goodsId;
                    string icon = GoodsModel.GetGoodsIcon(goodsId);
                    if (!string.IsNullOrEmpty(icon)) path = GameResPath.GetGoodsIconPath(icon);
                }
            }
            bool show = !string.IsNullOrEmpty(path) && await ResManager.SetImageAsync(head, path, nativeSize: false);
            if (this == null || _row != row) return;
            head.gameObject.SetActive(show);
            if (con != null) con.gameObject.SetActive(false);
        }
    }
}
