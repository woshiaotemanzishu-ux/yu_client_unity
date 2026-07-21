using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Bag;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝幻化的已激活列表与穿戴切换；配置、属性、升星和模型预览由后续轮次接入。</summary>
    public sealed class BabyIllusionView : BabyIllusionViewBind
    {
        private readonly List<GameObject> _items = new List<GameObject>();
        private bool _listening;
        private bool _shown;
        private int _selectedBabyId;
        private BaseAwardItem _activeCostItem;
        private BaseAwardItem _stageCostItem;
        private readonly List<BabyPropItem> _propItems = new List<BabyPropItem>();
        private FightingShowSmallItem _fightingItem;
        private int _pendingPowerBabyId;

        protected override void OnInit()
        {
            UIUtil.AddClick(useGp, OnUseClick);
            UIUtil.AddClick(activeBtn, OnFigureStarUpClick);
            UIUtil.AddClick(stageBtn, OnFigureStarUpClick);
            UIUtil.AddClick(leftBtn, () => SelectRelative(-1));
            UIUtil.AddClick(rightBtn, () => SelectRelative(1));
        }

        protected override void OnShow(object args)
        {
            _shown = true;
            Subscribe();
            Refresh();
            _ = EnsureConfigsAndRefreshAsync();
        }

        protected override void OnHide()
        {
            _shown = false;
            _pendingPowerBabyId = 0;
            Unsubscribe();
            ClearItems();
        }

        protected override void OnDispose()
        {
            _shown = false;
            _pendingPowerBabyId = 0;
            Unsubscribe();
            ClearItems();
        }

        private void Subscribe()
        {
            if (_listening) return;
            _listening = true;
            EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
        }

        private void Unsubscribe()
        {
            if (!_listening) return;
            _listening = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
        }

        private void OnBabyUpdate(int command)
        {
            if (!gameObject.activeInHierarchy) return;
            if (command == Proto.BABY_FIGURE_POWER
                && BabyModel.Instance.LastFigurePowerResult?.BabyId == _pendingPowerBabyId)
                _pendingPowerBabyId = 0;
            if (command == Proto.BABY_FIGURE_INFO || command == Proto.BABY_FIGURE_WEAR
                || command == Proto.BABY_FIGURE_STAR_UP || command == Proto.BABY_FIGURE_POWER)
                Refresh();
        }

        private void OnBagUpdate()
        {
            if (gameObject.activeInHierarchy) Refresh();
        }

        private void Refresh()
        {
            BabyModel model = BabyModel.Instance;
            int wornBabyId = model.Basic != null ? model.Basic.BabyId : 0;
            List<BabyFigureEntry> active = model.Figures != null ? model.Figures.ActiveList : null;
            if (BabyFigureConfigs.Get(_selectedBabyId) == null)
                _selectedBabyId = BabyFigureConfigs.Get(wornBabyId) != null ? wornBabyId
                    : BabyFigureConfigs.All.Count > 0 ? BabyFigureConfigs.All[0].BabyId : 0;

            ClearItems();
            if (_tpl_BabyIlluItem != null && illuGp != null)
            {
                for (int i = 0; i < BabyFigureConfigs.All.Count; i++)
                {
                    BabyFigureConfigs.BabyFigureCfg cfg = BabyFigureConfigs.All[i];
                    CreateItem(cfg, FindActiveEntry(active, cfg.BabyId));
                }
            }

            if (babyName != null)
            {
                BabyFigureConfigs.BabyFigureCfg cfg = BabyFigureConfigs.Get(_selectedBabyId);
                babyName.text = _selectedBabyId <= 0 ? string.Empty
                    : cfg != null ? cfg.BabyName : _selectedBabyId.ToString();
            }
            BabyFigureEntry selected = FindActiveEntry(active, _selectedBabyId);
            if (selectedImg != null) selectedImg.gameObject.SetActive(_selectedBabyId > 0 && wornBabyId == _selectedBabyId);
            if (useGp != null) useGp.gameObject.SetActive(selected != null);
            if (activeGp != null) activeGp.gameObject.SetActive(_selectedBabyId > 0 && selected == null);
            if (unActive != null) unActive.gameObject.SetActive(_selectedBabyId > 0 && selected == null);
            bool hasNextStar = selected != null && BabyFigureStarConfigs.IsLoaded
                && BabyFigureStarConfigs.Get(selected.BabyId, selected.BabyStar + 1) != null;
            if (stageGp != null) stageGp.gameObject.SetActive(hasNextStar);
            if (maxImg != null) maxImg.gameObject.SetActive(selected != null && BabyFigureStarConfigs.IsLoaded && !hasNextStar);
            BabyFigureConfigs.BabyFigureCfg selectedCfg = BabyFigureConfigs.Get(_selectedBabyId);
            UpdateCostItem(ref _activeCostItem, activeitemGp, activeLb, selected != null || selectedCfg == null || selectedCfg.Costs.Count == 0 ? 0 : selectedCfg.Costs[0].TypeId,
                selectedCfg != null && selectedCfg.Costs.Count > 0 ? selectedCfg.Costs[0].Num : 0);
            BabyFigureStarConfigs.BabyFigureStarCfg nextCfg = hasNextStar ? BabyFigureStarConfigs.Get(selected.BabyId, selected.BabyStar + 1) : null;
            UpdateCostItem(ref _stageCostItem, stageitemGp, stageLb, nextCfg != null && nextCfg.Costs.Count > 0 ? nextCfg.Costs[0].TypeId : 0,
                nextCfg != null && nextCfg.Costs.Count > 0 ? nextCfg.Costs[0].Num : 0);
            RefreshProps(selected);
            RefreshFighting(selected);
        }

        private void CreateItem(BabyFigureConfigs.BabyFigureCfg cfg, BabyFigureEntry entry)
        {
            GameObject go = Instantiate(_tpl_BabyIlluItem, illuGp, false);
            go.name = "BabyIlluItem_" + cfg.BabyId;
            go.SetActive(true);
            _items.Add(go);

            BabyIlluItem item = go.GetComponent<BabyIlluItem>();
            if (item == null) return;
            if (item.stageLb != null)
            {
                item.stageLb.gameObject.SetActive(entry != null);
                item.stageLb.text = entry != null ? entry.BabyStar.ToString() : string.Empty;
            }
            if (item.unActive != null) item.unActive.gameObject.SetActive(entry == null);
            item.SetStar(entry != null ? entry.BabyStar : 0, entry != null);
            if (item.select_img != null) item.select_img.gameObject.SetActive(cfg.BabyId == _selectedBabyId);
            if (item.resImg != null && cfg != null && !string.IsNullOrEmpty(cfg.ResourceId))
                _ = ResManager.SetImageAsync(item.resImg, GameResPath.GetIcon("baby", cfg.ResourceId), nativeSize: false);
            int babyId = cfg.BabyId;
            UIUtil.AddClick(item.clickGp, () => Select(babyId));
        }

        private async Task EnsureConfigsAndRefreshAsync()
        {
            await BabyFigureConfigs.EnsureLoaded();
            await BabyFigureStarConfigs.EnsureLoaded();
            await GoodsModel.EnsureLoaded();
            if (_shown) Refresh();
        }

        private void Select(int babyId)
        {
            if (BabyFigureConfigs.Get(babyId) == null || babyId == _selectedBabyId) return;
            _selectedBabyId = babyId;
            Refresh();
        }

        private void SelectRelative(int offset)
        {
            for (int i = 0; i < BabyFigureConfigs.All.Count; i++)
            {
                if (BabyFigureConfigs.All[i].BabyId != _selectedBabyId) continue;
                int next = i + offset;
                if (next >= 0 && next < BabyFigureConfigs.All.Count) Select(BabyFigureConfigs.All[next].BabyId);
                return;
            }
        }

        private void OnUseClick()
        {
            if (FindActiveEntry(BabyModel.Instance.Figures != null ? BabyModel.Instance.Figures.ActiveList : null, _selectedBabyId) == null) return;
            BabyBasicInfo basic = BabyModel.Instance.Basic;
            int type = basic != null && basic.BabyId == _selectedBabyId ? 2 : 1;
            BabyController.Instance.RequestSetFigure(type, _selectedBabyId);
        }

        private void OnFigureStarUpClick()
        {
            if (BabyFigureConfigs.Get(_selectedBabyId) != null) BabyController.Instance.RequestFigureStarUp(_selectedBabyId);
        }

        private void UpdateCostItem(ref BaseAwardItem item, RectTransform parent, TMPro.TextMeshProUGUI label, long typeId, long num)
        {
            if (typeId <= 0 || typeId > int.MaxValue)
            {
                if (item != null) item.gameObject.SetActive(false);
                if (label != null) { label.text = string.Empty; label.gameObject.SetActive(false); }
                return;
            }
            if (item == null && _tpl_BaseAwardItem != null && parent != null)
            {
                GameObject go = Instantiate(_tpl_BaseAwardItem, parent, false);
                item = go.GetComponent<BaseAwardItem>();
                if (item != null) item.SetScale(0.7f);
            }
            long have = BagModel.Instance.GetTypeGoodsNum((int)typeId);
            string color = have >= num ? "#0f9f00" : "#ff4f50";
            if (label != null)
            {
                label.text = GoodsModel.GetGoodsName((int)typeId) + "\n<color=" + color + ">(" + have + "/" + num + ")</color>";
                label.gameObject.SetActive(true);
            }
            if (item == null) return;
            item.gameObject.SetActive(true);
            item.SetData((int)typeId, num);
        }

        private void RefreshProps(BabyFigureEntry selected)
        {
            if (!BabyFigureStarConfigs.IsLoaded || _tpl_BabyPropItem == null || propGp == null)
            {
                HideProps();
                return;
            }
            int babyId = _selectedBabyId;
            BabyFigureStarConfigs.BabyFigureStarCfg currentCfg = selected != null
                ? BabyFigureStarConfigs.Get(babyId, selected.BabyStar) : null;
            BabyFigureStarConfigs.BabyFigureStarCfg nextCfg = BabyFigureStarConfigs.Get(babyId, selected != null ? selected.BabyStar + 1 : 1);
            List<BabyFigureStarConfigs.AttrItem> current = currentCfg != null ? currentCfg.BaseAttrs : null;
            List<BabyFigureStarConfigs.AttrItem> next = nextCfg != null ? nextCfg.BaseAttrs : null;
            List<BabyFigureStarConfigs.AttrItem> source = current != null && current.Count > 0 ? current : next;
            if (source == null || source.Count == 0)
            {
                HideProps();
                return;
            }
            while (_propItems.Count < source.Count)
            {
                GameObject go = Instantiate(_tpl_BabyPropItem, propGp, false);
                BabyPropItem item = go.GetComponent<BabyPropItem>();
                if (item == null)
                {
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                    break;
                }
                _propItems.Add(item);
            }
            for (int i = 0; i < _propItems.Count; i++)
            {
                BabyPropItem item = _propItems[i];
                bool visible = i < source.Count;
                if (item == null) continue;
                item.gameObject.SetActive(visible);
                if (!visible) continue;
                BabyFigureStarConfigs.AttrItem value = source[i];
                BabyFigureStarConfigs.AttrItem nextValue = FindAttr(next, value.AttrId);
                long currentValue = current != null && current.Count > 0 ? value.Value : 0;
                item.SetData(value.AttrId, currentValue, nextValue != null, nextValue != null ? nextValue.Value : 0);
            }
        }

        private void RefreshFighting(BabyFigureEntry selected)
        {
            if (_selectedBabyId <= 0)
            {
                if (_fightingItem != null) _fightingItem.gameObject.SetActive(false);
                return;
            }
            long power = 0;
            long nextPower = 0;
            bool hasData = false;
            bool inactive = selected == null;
            if (selected != null)
            {
                power = selected.Power;
                nextPower = selected.NextPower;
                hasData = power != 0 || nextPower != 0;
            }
            else
            {
                BabyFigurePowerResult result = BabyModel.Instance.LastFigurePowerResult;
                if (result != null && result.BabyId == _selectedBabyId)
                {
                    power = result.Power;
                    nextPower = result.NextPower;
                    hasData = true;
                }
            }
            if (!hasData)
            {
                if (_fightingItem != null) _fightingItem.gameObject.SetActive(false);
                if (_pendingPowerBabyId != _selectedBabyId)
                {
                    _pendingPowerBabyId = _selectedBabyId;
                    BabyController.Instance.RequestFigurePower(_selectedBabyId);
                }
                return;
            }
            if (_fightingItem == null && _tpl_FightingShowSmallItem != null && fight != null)
            {
                GameObject go = Instantiate(_tpl_FightingShowSmallItem, fight, false);
                _fightingItem = go.GetComponent<FightingShowSmallItem>();
            }
            if (_fightingItem == null) return;
            _fightingItem.gameObject.SetActive(true);
            _fightingItem.SetFighting(power);
            _fightingItem.SetFightingUp(System.Math.Max(nextPower - power, 0));
            if (inactive && BabyModel.Instance.LastFigurePowerResult != null && BabyModel.Instance.LastFigurePowerResult.BabyId == _selectedBabyId
                && _fightingItem._lb_fighting != null)
                _fightingItem._lb_fighting.text = power + "+";
        }

        private void HideProps()
        {
            for (int i = 0; i < _propItems.Count; i++) if (_propItems[i] != null) _propItems[i].gameObject.SetActive(false);
        }

        private static BabyFigureStarConfigs.AttrItem FindAttr(List<BabyFigureStarConfigs.AttrItem> attrs, int attrId)
        {
            if (attrs == null) return null;
            for (int i = 0; i < attrs.Count; i++) if (attrs[i].AttrId == attrId) return attrs[i];
            return null;
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                GameObject item = _items[i];
                if (item == null) continue;
                if (Application.isPlaying) Destroy(item);
                else DestroyImmediate(item);
            }
            _items.Clear();
        }

        private static bool ContainsActive(List<BabyFigureEntry> entries, int babyId)
        {
            if (entries == null || babyId <= 0) return false;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].IsActivated && entries[i].BabyId == babyId) return true;
            return false;
        }

        private static BabyFigureEntry FindActiveEntry(List<BabyFigureEntry> entries, int babyId)
        {
            if (entries == null || babyId <= 0) return null;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].IsActivated && entries[i].BabyId == babyId) return entries[i];
            return null;
        }

        private static int FirstActiveId(List<BabyFigureEntry> entries)
        {
            if (entries == null) return 0;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].IsActivated) return entries[i].BabyId;
            return 0;
        }
    }
}
