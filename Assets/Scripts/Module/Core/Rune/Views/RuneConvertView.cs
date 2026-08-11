using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Rune;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.RuneTreasure;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>劫魄宝库；列表与显隐只消费 RuneConfigs/RuneModel，视觉结构保留在 RuneModule.prefab。</summary>
    public sealed class RuneConvertView : RuneConvertViewBind
    {
        private const int RuneStoneTypeId = 36100002;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_RuneConvertItem != null) _tpl_RuneConvertItem.SetActive(false);
            if (_tpl_RuneTreasureMainView != null) _tpl_RuneTreasureMainView.SetActive(false);
            BindClick(closeBtn, Hide);
            BindClick(getBtn, () => { Hide(); RuneTreasureFlow.Open(); });
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            Render();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearRows();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            RuneModel.Instance.Changed += Render;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            RuneModel.Instance.Changed -= Render;
            _subscribed = false;
        }

        private void Render()
        {
            ClearRows();
            if (label1 != null) label1.text = "劫魄宝库";
            if (tips != null) tips.text = "劫魄寻幽可获得大量劫魄碎片！";
            if (own != null) own.text = "拥有";
            if (num != null) num.text = RuneModel.Instance.RuneChip.ToString();
            if (scroll_group == null || scroll_group.content == null || _tpl_RuneConvertItem == null) return;

            foreach (RuneConfigs.ExchangeRow value in RuneConfigs.ExchangeRows)
                CreateRow(value, scroll_group.content);
            scroll_group.StopMovement();
            scroll_group.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll_group.content);
        }

        private void CreateRow(RuneConfigs.ExchangeRow value, RectTransform parent)
        {
            GameObject clone = Instantiate(_tpl_RuneConvertItem, parent, false);
            clone.name = "RuneConvertItem_" + value.Id;
            clone.SetActive(true);
            RuneConvertItemBind row = clone.GetComponent<RuneConvertItemBind>()
                ?? clone.GetComponentInChildren<RuneConvertItemBind>(true);
            if (row == null) { Destroy(clone); return; }
            row.Show();
            _rows.Add(clone);

            GoodsModel.GoodsBasic goods = GoodsModel.GetGoodsBasicByTypeId(value.GoodsTypeId);
            if (row.goods_name != null)
                row.goods_name.text = goods?.Name ?? value.GoodsTypeId.ToString();
            int subtype = goods?.Subtype ?? 0;
            if (row.pro != null)
                row.pro.text = BuildAttributeText(RuneConfigs.GetComputedAttributes(
                    subtype, GoodsModel.GetColor(value.GoodsTypeId), 1));

            int floor = RuneModel.Instance.DungeonLevel?.Level ?? 0;
            bool unlocked = floor >= value.TowerFloor;
            if (row.condition != null)
                row.condition.text = unlocked ? string.Empty : "踏破九劫塔第" + value.TowerFloor + "层";
            if (row.btn_conta != null) row.btn_conta.gameObject.SetActive(unlocked);
            if (row.price != null) row.price.text = value.RuneChip.ToString();
            BindClick(row.buyBtn, () => Exchange(value));

            CreateAward(row._tpl_BaseAwardItem, row.icon_conta, value.GoodsTypeId, value.GoodsCount);
            CreateAward(row._tpl_BaseAwardItem, row.cost_icon, RuneStoneTypeId, 0);
        }

        private static void CreateAward(GameObject template, RectTransform parent, int typeId,
            long count)
        {
            if (template == null || parent == null || typeId <= 0) return;
            GameObject clone = Instantiate(template, parent, false);
            clone.SetActive(true);
            BaseAwardItem item = clone.GetComponent<BaseAwardItem>()
                ?? clone.GetComponentInChildren<BaseAwardItem>(true);
            if (item == null) { Destroy(clone); return; }
            item.Show();
            item.SetData(typeId, count);
        }

        private static string BuildAttributeText(IReadOnlyList<RuneConfigs.AttrValue> attrs)
        {
            if (attrs == null || attrs.Count == 0) return string.Empty;
            var lines = new List<string>(attrs.Count);
            for (int i = 0; i < attrs.Count; i++)
                lines.Add(GoodsModel.GetAttrName(attrs[i].AttrId) + " " +
                          GoodsModel.FormatAttrValue(attrs[i].AttrId, attrs[i].Value));
            return string.Join("\n", lines);
        }

        private static void Exchange(RuneConfigs.ExchangeRow value)
        {
            int floor = RuneModel.Instance.DungeonLevel?.Level ?? 0;
            if (floor < value.TowerFloor)
            {
                TipsManager.Toast("踏破九劫塔第" + value.TowerFloor + "层后开放");
                return;
            }
            if (RuneModel.Instance.RuneChip < value.RuneChip)
            {
                TipsManager.Toast("劫魄碎片不足");
                return;
            }
            RuneController.Instance.Exchange(value.Id);
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++) DestroyRuntimeRow(_rows[i]);
            _rows.Clear();
        }

        private static void DestroyRuntimeRow(GameObject row)
        {
            if (row == null) return;
            BaseView[] views = row.GetComponentsInChildren<BaseView>(true);
            for (int i = views.Length - 1; i >= 0; i--)
                if (views[i] != null && views[i].IsShown) views[i].Hide();
            Destroy(row);
        }

        private static void BindClick(Component target, Action action)
        {
            if (target == null || action == null) return;
            Image image = target as Image ?? target.GetComponent<Image>()
                ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ClearRows();
        }
    }
}
