using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Setting;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>装备吞噬：配置过滤可吞噬物，默认安全选择低评分 0/1 星装备，确认后发送真实 15025 列表。</summary>
    public sealed class BagSmeltView : BagSmeltViewBind
    {
        public override UILayer Layer => UILayer.Popup;

        private const int EssenceTypeId = 38250022;
        private const int MaxItems = 50;
        private const int Columns = 6;
        private const float CellStep = 92f;
        private const float ItemScale = 0.72f;

        private readonly List<BagGoods> _eligible = new List<BagGoods>();
        private readonly Dictionary<long, long> _selected = new Dictionary<long, long>();
        private readonly List<BaseAwardItem> _itemPool = new List<BaseAwardItem>();
        private bool _autoOn = true;
        private bool _oneStarOn = true;
        private bool _subscribed;
        private int _refreshEpoch;
        private int _autoFuseEpoch;
        private int _lifecycleEpoch;

        protected override void OnInit()
        {
            HideNode(_tpl_DownDropBtn);
            HideNode(_tpl_FightingShowSmallItem);
            BindBtn(closeBtn, Hide);
            BindBtn(propBtn, ShowProperties);
            BindBtn(useBtn, FuseSelected);
            BindBtn(autoGp, () => RequestAutoSmelt(!_autoOn));
            BindBtn(oneStarGp, () => SetOneStar(!_oneStarOn));
            RefreshAutoSetting();
            SetOneStar(true);
        }

        protected override void OnShow(object args)
        {
            _lifecycleEpoch++;
            Subscribe();
            RefreshAutoSetting();
            BagFusionController.Instance.RequestInfo();
            _ = RefreshAsync(true);
        }

        protected override void OnHide()
        {
            _lifecycleEpoch++;
            _refreshEpoch++;
            _autoFuseEpoch++;
            Unsubscribe();
            BagFlow.NotifyActivitySubHidden(this);
        }

        protected override void OnDispose()
        {
            _lifecycleEpoch++;
            _refreshEpoch++;
            _autoFuseEpoch++;
            Unsubscribe();
            foreach (BaseAwardItem item in _itemPool)
                if (item != null) ResManager.ReleaseInstance(item.gameObject);
            _itemPool.Clear();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
            EventDispatcher.On(GlobalEvent.EVT_SETTING_UPDATED, RefreshAutoSetting);
            EventDispatcher.On(GlobalEvent.EVT_BAG_FUSION_SUCCESS, OnFusionSuccess);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SETTING_UPDATED, RefreshAutoSetting);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_FUSION_SUCCESS, OnFusionSuccess);
            _subscribed = false;
        }

        private void OnBagUpdate() => _ = RefreshAsync(false);

        private void RefreshAutoSetting()
        {
            _autoOn = SettingModel.Get(4, 1, 0) == 1;
            if (select0 != null) select0.gameObject.SetActive(_autoOn);
            if (Unselect0 != null) Unselect0.gameObject.SetActive(!_autoOn);
        }

        private void RequestAutoSmelt(bool on)
        {
            if (on)
            {
                TipsManager.Confirm("勾选后将会自动吞噬橙色一星及以下、评分不高于身上装备的装备，是否开启？",
                    () => SettingController.Instance.SendSetting(4, 1, 1));
            }
            else
            {
                SettingController.Instance.SendSetting(4, 1, 0);
            }
        }

        private void OnFusionSuccess()
        {
            _selected.Clear();
            Render();
            if (!_autoOn) return;
            int epoch = ++_autoFuseEpoch;
            _ = ContinueAutoFuse(epoch);
        }

        private async Task ContinueAutoFuse(int epoch)
        {
            await TimeUtil.Delay(800);
            if (epoch != _autoFuseEpoch || !IsShown || !_autoOn) return;
            await RefreshAsync(true);
            if (epoch != _autoFuseEpoch || !IsShown || !_autoOn || _selected.Count == 0) return;
            FuseSelected();
        }

        private async Task RefreshAsync(bool resetSelection)
        {
            int epoch = ++_refreshEpoch;
            await Task.WhenAll(GoodsModel.EnsureLoaded(), BagFusionConfigs.EnsureLoaded());
            if (epoch != _refreshEpoch || !IsShown) return;

            _eligible.Clear();
            foreach (BagGoods goods in BagModel.Instance.BagGoodsList)
                if (BagFusionConfigs.TryGetFusionExp(goods.TypeId, out _)) _eligible.Add(goods);

            if (resetSelection || _autoOn) SelectSafeDefaults();
            else
            {
                var valid = new HashSet<long>();
                foreach (BagGoods goods in _eligible) valid.Add(goods.GoodsId);
                var remove = new List<long>();
                foreach (long id in _selected.Keys) if (!valid.Contains(id)) remove.Add(id);
                foreach (long id in remove) _selected.Remove(id);
            }

            await EnsurePoolAsync(_eligible.Count, epoch);
            if (epoch != _refreshEpoch || !IsShown) return;
            Render();
        }

        private void SelectSafeDefaults()
        {
            _selected.Clear();
            int career = RoleModel.Instance.Career;
            int sex = RoleModel.Instance.Sex;
            foreach (BagGoods goods in _eligible)
            {
                if (_selected.Count >= MaxItems) break;
                if (goods.TypeId == EssenceTypeId)
                {
                    _selected[goods.GoodsId] = goods.GoodsNum;
                    continue;
                }

                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
                GoodsModel.EquipAttr equip = GoodsModel.GetEquipAttr(goods.TypeId);
                if (basic == null || equip == null || basic.EquipType == 7 || basic.EquipType == 9) continue;
                if (equip.Stage > 99 || equip.Star > 1 || (equip.Star == 1 && !_oneStarOn) || basic.Color > 4) continue;

                bool incompatible = (basic.CareerId != 0 && basic.CareerId != career)
                                    || (basic.Sex != 0 && basic.Sex != sex);
                BagGoods worn = EquipAutoWear.GetWorn(basic.EquipType);
                bool weakerThanWorn = worn != null && worn.Rating >= goods.Rating;
                if (incompatible || weakerThanWorn) _selected[goods.GoodsId] = goods.GoodsNum;
            }
        }

        private async Task EnsurePoolAsync(int count, int epoch)
        {
            if (_Scroller1 == null || _Scroller1.content == null) return;
            string key = GameResPath.GetUIPrefab("common", "BaseAwardItem");
            while (_itemPool.Count < count)
            {
                GameObject go = await ResManager.InstantiateAsync(key, _Scroller1.content);
                if (epoch != _refreshEpoch)
                {
                    if (go != null) ResManager.ReleaseInstance(go);
                    return;
                }
                if (go == null) return;
                BaseAwardItem item = go.GetComponent<BaseAwardItem>();
                if (item == null)
                {
                    ResManager.ReleaseInstance(go);
                    return;
                }
                go.name = "BagSmeltItem_" + _itemPool.Count;
                item.SetScale(ItemScale);
                _itemPool.Add(item);
            }
        }

        private void Render()
        {
            int rows = Mathf.CeilToInt(_eligible.Count / (float)Columns);
            if (_Scroller1 != null && _Scroller1.content != null)
                _Scroller1.content.sizeDelta = new Vector2(_Scroller1.content.sizeDelta.x, rows * CellStep);
            for (int i = 0; i < _itemPool.Count; i++)
            {
                BaseAwardItem item = _itemPool[i];
                bool active = i < _eligible.Count;
                item.gameObject.SetActive(active);
                if (!active) continue;
                BagGoods goods = _eligible[i];
                var rt = (RectTransform)item.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(i % Columns * CellStep, -(i / Columns) * CellStep);
                BagGoods captured = goods;
                item.SetClickCallBack(() => Toggle(captured));
                item.SetData(goods.TypeId, goods.GoodsNum, goods.Bind != 0, _selected.ContainsKey(goods.GoodsId));
            }
            if (nothingLb != null) nothingLb.gameObject.SetActive(_eligible.Count == 0);
            RefreshExpText();
        }

        private void Toggle(BagGoods goods)
        {
            if (goods == null) return;
            if (_selected.ContainsKey(goods.GoodsId)) _selected.Remove(goods.GoodsId);
            else if (_selected.Count >= MaxItems) TipsManager.Toast("单次最多选择 " + MaxItems + " 件");
            else _selected[goods.GoodsId] = goods.GoodsNum;
            Render();
        }

        private void RefreshExpText()
        {
            long add = 0;
            foreach (BagGoods goods in _eligible)
            {
                if (!_selected.TryGetValue(goods.GoodsId, out long num)) continue;
                if (BagFusionConfigs.TryGetFusionExp(goods.TypeId, out long exp))
                    add += exp * (goods.TypeId == EssenceTypeId ? num : 1L);
            }
            long need = BagFusionConfigs.GetLevelNeed(BagFusionController.FusionLv);
            if (lvLb != null) lvLb.text = "Lv." + BagFusionController.FusionLv;
            if (expLb != null) expLb.text = need > 0 ? BagFusionController.FusionExp + "/" + need : BagFusionController.FusionExp.ToString();
            if (curLb != null) curLb.text = "本次吞噬经验：<color=#00b11d>" + add + "</color>";
        }

        private void FuseSelected()
        {
            if (_selected.Count == 0)
            {
                TipsManager.Toast("当前没有选择可以吞噬的道具");
                return;
            }
            var list = new List<(long goodsId, long num)>();
            foreach (BagGoods goods in _eligible)
                if (_selected.TryGetValue(goods.GoodsId, out long num)) list.Add((goods.GoodsId, num));
            BagFusionController.Instance.Fuse(list);
        }

        private async void ShowProperties()
        {
            int lifecycleEpoch = _lifecycleEpoch;
            await Task.WhenAll(BagFusionConfigs.EnsureLoaded(), GoodsModel.EnsureLoaded());
            if (!IsShown || lifecycleEpoch != _lifecycleEpoch) return;

            int level = BagFusionController.FusionLv;
            IReadOnlyList<(int attrId, long value)> attrs = BagFusionConfigs.GetLevelAttrValues(level);
            var text = new StringBuilder();
            for (int i = 0; i < attrs.Count; i++)
            {
                (int attrId, long value) attr = attrs[i];
                if (i > 0) text.Append('\n');
                string name = GoodsModel.GetAttrName(attr.attrId);
                if (string.IsNullOrEmpty(name)) name = "属性" + attr.attrId;
                text.Append(name).Append(" <color=#0a953e> + ")
                    .Append(GoodsModel.FormatAttrValue(attr.attrId, attr.value)).Append("</color>");
            }
            if (text.Length == 0) text.Append("暂无属性加成");

            BagFlow.OpenSub("SmeltPropView", new SmeltPropView.Presentation
            {
                Title = "属性加成",
                Text = text.ToString(),
            });
        }

        private void SetOneStar(bool on)
        {
            _oneStarOn = on;
            if (select1 != null) select1.gameObject.SetActive(on);
            if (Unselect1 != null) Unselect1.gameObject.SetActive(!on);
            if (IsShown) _ = RefreshAsync(true);
        }

        private static void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            GameObject go = target.gameObject;
            foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            image.raycastTarget = true;
            UIUtil.AddClick(image, onClick);
        }

        private static void HideNode(GameObject go)
        {
            if (go != null) go.SetActive(false);
        }
    }
}
