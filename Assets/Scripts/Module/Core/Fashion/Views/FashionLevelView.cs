using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>时装部位升级弹窗；候选物只取“已满阶时装/颜色”的真实背包实例。</summary>
    public sealed class FashionLevelView : FashionLevelViewBind
    {
        private const float ItemStep = 106f;

        private sealed class Source
        {
            public BagGoods Goods;
            public int Count;
            public int ExpPer;
            public bool Selected;
        }

        private readonly List<Source> _sources = new List<Source>();
        private readonly List<FasBagItemRenderer> _cells = new List<FasBagItemRenderer>();
        private int _posId = 1;
        private bool _subscribed;

        public override UILayer Layer => UILayer.Popup;

        protected override void OnInit()
        {
            if (flv_closebut_image != null) UIUtil.AddClick(flv_closebut_image, Hide);
            if (flv_devour_but != null) UIUtil.AddClick(flv_devour_but, Submit);
        }

        protected override void OnShow(object args)
        {
            _posId = args is int p && p == 1 ? p : 1; // 服务端实际只开放衣服部位升级。
            Subscribe();
            _ = LoadThenRefresh();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private async System.Threading.Tasks.Task LoadThenRefresh()
        {
            await FashionConfigs.EnsureLoaded();
            await GoodsModel.EnsureLoaded();
            if (this == null || !gameObject.activeInHierarchy) return;
            Refresh();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_FASHION_UPDATE, OnUpdated);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_FASHION_UPDATE, OnUpdated);
        }

        private void OnUpdated()
        {
            if (this != null && gameObject.activeInHierarchy) Refresh();
        }

        private void Refresh()
        {
            FashionModel.PosInfo pos = FashionModel.Instance.GetPos(_posId);
            int lv = pos?.PosLv ?? 0;
            long exp = pos?.PosUpgradeNum ?? 0;
            FashionConfigs.PositionRow row = FashionConfigs.GetPositionRow(_posId, lv);
            FashionConfigs.PositionRow next = FashionConfigs.GetPositionRow(_posId, lv + 1);

            string part = _posId == 1 ? "时装" : "发饰";
            if (_lb_1 != null) _lb_1.text = part + "等级：";
            if (flv_level_label != null) flv_level_label.text = "Lv." + lv;
            if (tips_label != null) tips_label.text = "吞噬多余" + part + "可提升" + part + "等级";
            if (tips_label1 != null) tips_label1.text = "未升至满阶的" + part + "不能进行吞噬";

            long cost = row?.Cost ?? 0;
            bool max = row == null || next == null || cost <= 0;
            if (exp_label != null) exp_label.text = max ? "已满级" : (exp + "/" + cost);
            if (exp_num_image != null)
            {
                float ratio = max ? 1f : Mathf.Clamp01((float)exp / cost);
                exp_num_image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 533f * ratio);
            }
            RefreshAttrs(row, next);
            BuildSources(max, Math.Max(0L, cost - exp));
            RefreshCells();
        }

        private void RefreshAttrs(FashionConfigs.PositionRow current, FashionConfigs.PositionRow next)
        {
            TextMeshProUGUI[] names = { flv_attr_dec0_label, flv_attr_dec1_label, flv_attr_dec2_label };
            TextMeshProUGUI[] cur = { flv_att0_1_label, flv_att0_2_label, flv_att0_3_label };
            TextMeshProUGUI[] nxt = { flv_att1_1_label, flv_att1_2_label, flv_att1_3_label };
            for (int i = 0; i < names.Length; i++)
            {
                FashionConfigs.AttrValue a = current != null && i < current.AttrAdds.Count ? current.AttrAdds[i] : null;
                FashionConfigs.AttrValue b = next != null && i < next.AttrAdds.Count ? next.AttrAdds[i] : null;
                int attrId = a?.AttrId ?? b?.AttrId ?? 0;
                string attrName = attrId > 0 ? GoodsModel.GetAttrName(attrId) : string.Empty;
                if (names[i] != null) names[i].text = string.IsNullOrEmpty(attrName) ? "属性" : attrName;
                if (cur[i] != null) cur[i].text = a == null ? "+0" : ("+" + GoodsModel.FormatAttrValue(a.AttrId, a.Value));
                if (nxt[i] != null) nxt[i].text = b == null ? "已满阶" : ("+" + GoodsModel.FormatAttrValue(b.AttrId, b.Value));
            }
        }

        private void BuildSources(bool maxPosition, long needExp)
        {
            _sources.Clear();
            if (maxPosition || needExp <= 0) return;

            var allowed = new Dictionary<int, int>(); // typeId -> 单件经验
            int career = RoleModel.Instance.Career;
            int sex = RoleModel.Instance.Sex;
            foreach (int fashionId in FashionConfigs.GetFashionIds(_posId))
            {
                FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(_posId, fashionId);
                if (entry == null) continue;
                FashionConfigs.ModelRow modelRow = FashionConfigs.GetModelRow(_posId, fashionId, career, sex, 0);
                int expPer = modelRow?.Exp ?? 0;

                int baseLv = entry.GetStarLv(0);
                if (baseLv > 0 && FashionConfigs.GetRow(_posId, fashionId, 0, baseLv).Found
                    && !FashionConfigs.GetRow(_posId, fashionId, 0, baseLv + 1).Found)
                {
                    allowed[fashionId] = expPer;
                }

                foreach (int colorId in FashionConfigs.GetColorIds(_posId, fashionId))
                {
                    if (!entry.IsColorUnlocked(colorId)) continue;
                    int colorLv = entry.GetStarLv(colorId);
                    if (colorLv <= 0 || !FashionConfigs.GetRow(_posId, fashionId, colorId, colorLv).Found
                        || FashionConfigs.GetRow(_posId, fashionId, colorId, colorLv + 1).Found) continue;
                    List<(int type, int typeId, long num)> activation =
                        FashionConfigs.ParseCostList(FashionConfigs.GetRow(_posId, fashionId, colorId, 1).ActiveCostJson);
                    if (activation.Count > 0 && activation[0].typeId > 0) allowed[activation[0].typeId] = expPer;
                }
            }

            foreach (BagGoods goods in BagModel.Instance.BagGoodsList)
            {
                if (needExp <= 0) break;
                if (goods == null || goods.GoodsId <= 0 || goods.GoodsNum <= 0
                    || !allowed.TryGetValue(goods.TypeId, out int expPer) || expPer <= 0) continue;

                int count = GetCandidateCount(needExp, expPer, goods.GoodsNum);
                if (count <= 0) continue;
                _sources.Add(new Source { Goods = goods, Count = count, ExpPer = expPer, Selected = true });
                needExp = Math.Max(0L, needExp - (long)expPer * count);
            }
        }

        /// <summary>
        /// 默认只选够当前级升下一级的数量；最后一堆按单件经验向上取整，
        /// 并同时受真实库存与 41305 u16 数量字段限制。
        /// </summary>
        private static int GetCandidateCount(long needExp, int expPer, long inventory)
        {
            if (needExp <= 0 || expPer <= 0 || inventory <= 0) return 0;
            long wireSafeInventory = Math.Min(inventory, ushort.MaxValue);
            long required = needExp / expPer + (needExp % expPer == 0 ? 0 : 1);
            return (int)Math.Min(wireSafeInventory, required);
        }

        private void RefreshCells()
        {
            Transform parent = flv_scroller != null ? (flv_scroller.content != null ? flv_scroller.content : flv_scroller.transform) : null;
            if (parent == null || _tpl_FasBagItemRenderer == null) return;
            int visible = Math.Max(8, _sources.Count);
            while (_cells.Count < visible)
            {
                GameObject go = Instantiate(_tpl_FasBagItemRenderer, parent);
                go.SetActive(true);
                FasBagItemRenderer cell = go.GetComponent<FasBagItemRenderer>();
                if (cell == null) { Destroy(go); break; }
                _cells.Add(cell);
            }
            for (int i = 0; i < _cells.Count; i++)
            {
                FasBagItemRenderer cell = _cells[i];
                bool show = i < visible;
                cell.gameObject.SetActive(show);
                if (!show) continue;
                RectTransform rt = cell.transform as RectTransform;
                if (rt != null) rt.anchoredPosition = new Vector2((i % 4) * ItemStep, -(i / 4) * ItemStep);
                if (i < _sources.Count)
                {
                    int captured = i;
                    Source src = _sources[i];
                    cell.SetData(src.Goods, src.Count, src.Selected, () => Toggle(captured));
                }
                else cell.SetData(null, 0, false, null);
            }
            if (flv_scroller != null && flv_scroller.content != null)
            {
                Vector2 size = flv_scroller.content.sizeDelta;
                size.y = Mathf.Max(flv_scroller.viewport != null ? flv_scroller.viewport.rect.height : 0f,
                    Mathf.CeilToInt(visible / 4f) * ItemStep);
                flv_scroller.content.sizeDelta = size;
            }
            RefreshSelectedTotal();
        }

        private void Toggle(int index)
        {
            if (index < 0 || index >= _sources.Count) return;
            _sources[index].Selected = !_sources[index].Selected;
            RefreshCells();
        }

        private void RefreshSelectedTotal()
        {
            long exp = 0;
            int count = 0;
            foreach (Source src in _sources)
            {
                if (!src.Selected) continue;
                count += src.Count;
                exp += (long)src.ExpPer * src.Count;
            }
            if (flv_add_exp_label != null)
                flv_add_exp_label.text = exp > 0 ? exp.ToString() : (count > 0 ? ("已选" + count + "件") : "0");
        }

        private void Submit()
        {
            var list = new List<(long goodsId, int num)>();
            foreach (Source src in _sources)
                if (src.Selected && src.Goods != null) list.Add((src.Goods.GoodsId, src.Count));
            if (list.Count == 0)
            {
                FashionConfigs.PositionRow row = FashionConfigs.GetPositionRow(_posId, FashionModel.Instance.GetPos(_posId)?.PosLv ?? 0);
                TipsManager.Toast(row == null || row.Cost <= 0 ? "已满级" : "材料不足");
                return;
            }
            FashionController.Instance.UpgradePosition(_posId, list);
        }
    }
}
