using System.Collections.Generic;
using System.Linq;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Rune;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>穿戴灵魄总属性；直接汇总 16700 attr_list，不用静态配置反推运行态。</summary>
    public sealed class RunePropertyView : RunePropertyViewBind
    {
        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_RunePropertyItem != null) _tpl_RunePropertyItem.SetActive(false);
            if (property_bg != null)
            {
                property_bg.raycastTarget = true;
                UIUtil.AddClick(property_bg, Hide);
            }
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            Render();
        }

        protected override void OnHide() => Unsubscribe();

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
            var totals = new Dictionary<int, long>();
            foreach (RuneModel.SlotVo slot in RuneModel.Instance.Slots)
            {
                if (!slot.IsWorn || slot.Attrs == null) continue;
                foreach (RuneModel.RuneAttrVo attr in slot.Attrs)
                    totals[attr.AttrId] = totals.TryGetValue(attr.AttrId, out long old) ? old + attr.AttrNum : attr.AttrNum;
            }
            if (property_title != null) property_title.text = "总战力 " + RuneModel.Instance.SumPower;
            if (none_conta != null) none_conta.gameObject.SetActive(totals.Count == 0);
            if (tips != null) tips.text = totals.Count == 0 ? "暂未镶嵌灵魄" : string.Empty;
            if (Content == null || _tpl_RunePropertyItem == null) return;
            foreach (KeyValuePair<int, long> pair in totals.OrderBy(value => value.Key))
            {
                GameObject clone = Instantiate(_tpl_RunePropertyItem, Content, false);
                clone.name = "RuneProperty_" + pair.Key;
                clone.SetActive(true);
                RunePropertyItemBind row = clone.GetComponent<RunePropertyItemBind>()
                    ?? clone.GetComponentInChildren<RunePropertyItemBind>(true);
                if (row == null) { Destroy(clone); continue; }
                row.Show();
                if (row.pro_name != null) row.pro_name.text = GoodsModel.GetAttrName(pair.Key);
                if (row.value != null) row.value.text = GoodsModel.FormatAttrValue(pair.Key, pair.Value);
                _rows.Add(clone);
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            if (scroll != null)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                GameObject row = _rows[i];
                if (row == null) continue;
                BaseView[] views = row.GetComponentsInChildren<BaseView>(true);
                for (int j = views.Length - 1; j >= 0; j--)
                    if (views[j] != null && views[j].IsShown) views[j].Hide();
                Destroy(row);
            }
            _rows.Clear();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ClearRows();
        }
    }
}
