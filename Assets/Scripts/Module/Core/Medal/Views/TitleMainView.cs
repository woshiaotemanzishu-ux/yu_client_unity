using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Title;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Medal
{
    /// <summary>
    /// 人物→境界→天境。页面结构来自老端同账号运行时快照首次烤制的 TitleMainView.prefab；
    /// 本类只填权威 13405 数据、配置、交互与特效，不在运行时代码中重建视觉坐标。
    /// </summary>
    public sealed class TitleMainView : TitleMainViewBind
    {
        private readonly List<TitleItem> _titleItems = new List<TitleItem>();
        private readonly List<TitleAttrItem> _attributeItems = new List<TitleAttrItem>();
        private uint _selectedId;
        private bool _listening;
        private BaseAwardItem _costItem;
        private FightingShowSmallItem _fighting;
        private UIEffectStage.Handle _mainEffect;
        private UIEffectStage.Handle _successEffect;
        private int _mainEffectEpoch;
        private int _successEffectEpoch;
        private int _currentShowId;
        private float _lastWriteClickAt = -10f;

        public static bool HasAnyRed()
        {
            IReadOnlyList<MedalModel.TitleEntry> entries = MedalModel.Instance.TitleEntries;
            for (int i = 0; i < entries.Count; i++)
                if (CanUpgrade(entries[i])) return true;
            return false;
        }

        protected override void OnInit()
        {
            if (_tpl_TitleItem != null) _tpl_TitleItem.SetActive(false);
            if (_tpl_TitleAttrItem != null) _tpl_TitleAttrItem.SetActive(false);
            _costItem = item_gp != null ? item_gp.GetComponentInChildren<BaseAwardItem>(true) : null;
            _fighting = _gp_fight != null
                ? _gp_fight.GetComponentInChildren<FightingShowSmallItem>(true)
                : null;
            if (num_lb != null) num_lb.richText = true;
            BindClick(illsion_gp, OnIllusionClicked);
            BindClick(up_level_gp, OnUpgradeClicked);
        }

        protected override void OnShow(object args)
        {
            StartListening();
            if (_costItem != null) _costItem.Show();
            if (_fighting != null) _fighting.Show();
            RefreshAll();
        }

        protected override void OnHide()
        {
            StopListening();
            ClearTitleItems();
            ClearAttributeItems();
            ReleaseMainEffect();
            ReleaseSuccessEffect();
        }

        protected override void OnDispose()
        {
            StopListening();
            ClearTitleItems();
            ClearAttributeItems();
            ReleaseMainEffect();
            ReleaseSuccessEffect();
            base.OnDispose();
        }

        private void OnDestroy()
        {
            StopListening();
            ClearTitleItems();
            ClearAttributeItems();
            ReleaseMainEffect();
            ReleaseSuccessEffect();
        }

        private void StartListening()
        {
            if (_listening) return;
            MedalModel.Instance.Changed += RefreshAll;
            MedalModel.Instance.TitleOperationCompleted += OnTitleOperationCompleted;
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshAll);
            _listening = true;
        }

        private void StopListening()
        {
            if (!_listening) return;
            MedalModel.Instance.Changed -= RefreshAll;
            MedalModel.Instance.TitleOperationCompleted -= OnTitleOperationCompleted;
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshAll);
            _listening = false;
        }

        private void RefreshAll()
        {
            if (!isActiveAndEnabled || !TitleConfigs.IsLoaded) return;
            RebuildTitleList();
            RenderSelected();
        }

        private void RebuildTitleList()
        {
            ClearTitleItems();
            if (Content == null || Content.content == null || _tpl_TitleItem == null) return;

            List<MedalModel.TitleEntry> entries = MedalModel.Instance.TitleEntries
                .Where(entry => entry != null && TitleConfigs.GetFirst(entry.Id) != null)
                .OrderBy(entry => entry.Id)
                .ToList();
            if (entries.Count == 0) return;

            if (_selectedId == 0 || entries.All(entry => entry.Id != _selectedId))
                _selectedId = ResolveInitialSelection(entries);

            for (int i = 0; i < entries.Count; i++)
            {
                MedalModel.TitleEntry entry = entries[i];
                GameObject go = Instantiate(_tpl_TitleItem, Content.content, false);
                go.name = "TitleItem_" + entry.Id;
                TitleItem item = go.GetComponent<TitleItem>();
                if (item == null)
                {
                    DestroyRuntime(go);
                    continue;
                }
                go.SetActive(true);
                item.Show();
                item.SetData(entry, entry.Id == _selectedId, CanUpgrade(entry), SelectTitle);
                _titleItems.Add(item);
            }

            Content.StopMovement();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content.content);
            int selectedIndex = entries.FindIndex(entry => entry.Id == _selectedId);
            Content.horizontalNormalizedPosition = entries.Count <= 1
                ? 0f
                : Mathf.Clamp01(selectedIndex / (float)(entries.Count - 1));
        }

        private uint ResolveInitialSelection(IReadOnlyList<MedalModel.TitleEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
                if (CanUpgrade(entries[i])) return entries[i].Id;
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].IsEquip != 0) return entries[i].Id;
            return entries[0].Id;
        }

        private void SelectTitle(uint id)
        {
            if (id == 0 || id == _selectedId) return;
            _selectedId = id;
            for (int i = 0; i < _titleItems.Count; i++)
                _titleItems[i].SetSelected(_titleItems[i].name == "TitleItem_" + id);
            RenderSelected();
        }

        private void RenderSelected()
        {
            MedalModel.TitleEntry entry = FindSelected();
            if (entry == null) return;
            bool active = entry.IsEquip != 0;
            TitleConfigs.Row current = TitleConfigs.Get(entry.Id, active ? entry.Level : (ushort)0)
                ?? TitleConfigs.GetFirst(entry.Id);
            if (current == null) return;
            TitleConfigs.Row next = active ? TitleConfigs.GetNext(entry.Id, entry.Level) : current;
            bool max = active && next == null;

            if (name_lb != null) name_lb.text = current.Name;
            if (detail_lb != null) detail_lb.text = current.Description;
            if (_fighting != null) _fighting.SetFighting(entry.Power);
            SetNode(select_illsion, entry.IsEquip == 2);
            SetNode(max_img, max);
            SetNode(up_level_gp, !max);
            SetNode(num_lb, !max);
            if (up_level_lb != null) up_level_lb.text = active ? "升星" : "激活";
            SetNode(red_img, CanUpgrade(entry));

            TitleConfigs.Row costSource = active ? next : current;
            RenderCost(costSource, max);
            RenderAttributes(entry, current, next, max);
            RestartMainEffect(current.ShowId);
        }

        private void RenderCost(TitleConfigs.Row row, bool max)
        {
            TitleConfigs.CostValue cost = row?.Costs.FirstOrDefault();
            if (cost == null)
            {
                if (num_lb != null) num_lb.text = string.Empty;
                return;
            }
            if (_costItem != null)
            {
                _costItem.SetScale(69f / 127f);
                _costItem.SetData(cost.TypeId, cost.Count);
            }
            long have = BagModel.Instance.GetTypeGoodsNum(cost.TypeId);
            string color = have >= cost.Count ? "#b3ff48" : "#ff4f50";
            if (num_lb != null)
                num_lb.text = max ? string.Empty : "<color=" + color + ">" + have + "</color>/" + cost.Count;
        }

        private void RenderAttributes(MedalModel.TitleEntry entry, TitleConfigs.Row current,
            TitleConfigs.Row next, bool max)
        {
            ClearAttributeItems();
            RectTransform parent = _Scroller1 != null && _Scroller1.content != null
                ? _Scroller1.content
                : Content1;
            if (parent == null || _tpl_TitleAttrItem == null) return;

            bool active = entry.IsEquip != 0;
            IReadOnlyList<TitleConfigs.AttributeValue> source = current.Attributes;
            for (int i = 0; i < source.Count; i++)
            {
                TitleConfigs.AttributeValue attr = source[i];
                long now = active ? attr.Value : 0L;
                long nextValue = 0L;
                if (!max && next != null)
                {
                    TitleConfigs.AttributeValue nextAttr = next.Attributes.FirstOrDefault(value => value.Id == attr.Id);
                    if (nextAttr != null) nextValue = nextAttr.Value;
                }

                GameObject go = Instantiate(_tpl_TitleAttrItem, parent, false);
                go.name = "TitleAttrItem_" + attr.Id;
                TitleAttrItem item = go.GetComponent<TitleAttrItem>();
                if (item == null)
                {
                    DestroyRuntime(go);
                    continue;
                }
                go.SetActive(true);
                item.Show();
                item.SetData(attr.Id, now, nextValue, !max);
                _attributeItems.Add(item);
            }
            if (_Scroller1 != null)
            {
                _Scroller1.StopMovement();
                _Scroller1.verticalNormalizedPosition = 1f;
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        private void OnUpgradeClicked()
        {
            MedalModel.TitleEntry entry = FindSelected();
            if (entry == null) return;
            TitleConfigs.Row target = entry.IsEquip == 0
                ? TitleConfigs.GetFirst(entry.Id)
                : TitleConfigs.GetNext(entry.Id, entry.Level);
            if (target == null)
            {
                TipsManager.Toast("天境称号已满星");
                return;
            }
            TitleConfigs.CostValue cost = target.Costs.FirstOrDefault();
            if (cost != null && BagModel.Instance.GetTypeGoodsNum(cost.TypeId) < cost.Count)
            {
                TipsManager.Toast("升星材料不足");
                return;
            }
            if (Time.unscaledTime - _lastWriteClickAt < 1f) return;
            _lastWriteClickAt = Time.unscaledTime;
            MedalController.Instance.RequestTitleUpgrade(entry.Id);
        }

        private void OnIllusionClicked()
        {
            MedalModel.TitleEntry entry = FindSelected();
            if (entry == null) return;
            if (entry.IsEquip == 0)
            {
                TipsManager.Toast("请先激活该天境称号");
                return;
            }
            if (Time.unscaledTime - _lastWriteClickAt < 1f) return;
            _lastWriteClickAt = Time.unscaledTime;
            if (entry.IsEquip == 2) MedalController.Instance.RequestTitleUnequip();
            else MedalController.Instance.RequestTitleEquip(entry.Id);
        }

        private void OnTitleOperationCompleted(MedalModel.TitleOperationKind kind, uint id,
            ushort level, uint code)
        {
            if (code != 1)
            {
                TipsManager.Toast("天境操作失败（错误码 " + code + "）");
                return;
            }
            if (kind == MedalModel.TitleOperationKind.Upgrade)
                PlaySuccessEffect(level == 0 ? "ui_ZiTi_Jihuo" : "ui_shengxingchengong");
        }

        private void RestartMainEffect(int showId)
        {
            if (_currentShowId == showId && _mainEffect != null) return;
            ReleaseMainEffect();
            _currentShowId = showId;
            string effectName = TitleConfigs.EffectName(showId);
            if (title_effect_gp == null || string.IsNullOrEmpty(effectName)) return;
            int epoch = _mainEffectEpoch;
            _ = AttachMainEffectAsync(effectName, epoch);
        }

        private async Task AttachMainEffectAsync(string effectName, int epoch)
        {
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                effectName, title_effect_gp, Vector2.zero, Vector3.one * 3.5f);
            if (this == null || epoch != _mainEffectEpoch || !_listening)
            {
                handle?.Dispose();
                return;
            }
            _mainEffect = handle;
            if (handle == null) GameLog.Warn("Title", "主称号特效加载失败: {0}", effectName);
        }

        private void PlaySuccessEffect(string effectName)
        {
            ReleaseSuccessEffect();
            if (success_effect_gp == null) return;
            int epoch = _successEffectEpoch;
            _ = AttachSuccessEffectAsync(effectName, epoch);
        }

        private async Task AttachSuccessEffectAsync(string effectName, int epoch)
        {
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                effectName, success_effect_gp, Vector2.zero, Vector3.one);
            if (this == null || epoch != _successEffectEpoch || !_listening)
            {
                handle?.Dispose();
                return;
            }
            _successEffect = handle;
        }

        private MedalModel.TitleEntry FindSelected()
        {
            IReadOnlyList<MedalModel.TitleEntry> entries = MedalModel.Instance.TitleEntries;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Id == _selectedId) return entries[i];
            return null;
        }

        private static bool CanUpgrade(MedalModel.TitleEntry entry)
        {
            if (entry == null || !TitleConfigs.IsLoaded) return false;
            TitleConfigs.Row target = entry.IsEquip == 0
                ? TitleConfigs.GetFirst(entry.Id)
                : TitleConfigs.GetNext(entry.Id, entry.Level);
            if (target == null || target.Costs.Count == 0) return false;
            for (int i = 0; i < target.Costs.Count; i++)
            {
                TitleConfigs.CostValue cost = target.Costs[i];
                if (BagModel.Instance.GetTypeGoodsNum(cost.TypeId) < cost.Count) return false;
            }
            return true;
        }

        private void ClearTitleItems()
        {
            for (int i = 0; i < _titleItems.Count; i++)
                if (_titleItems[i] != null) DestroyRuntime(_titleItems[i].gameObject);
            _titleItems.Clear();
        }

        private void ClearAttributeItems()
        {
            for (int i = 0; i < _attributeItems.Count; i++)
                if (_attributeItems[i] != null) DestroyRuntime(_attributeItems[i].gameObject);
            _attributeItems.Clear();
        }

        private void ReleaseMainEffect()
        {
            ++_mainEffectEpoch;
            _mainEffect?.Dispose();
            _mainEffect = null;
            _currentShowId = 0;
        }

        private void ReleaseSuccessEffect()
        {
            ++_successEffectEpoch;
            _successEffect?.Dispose();
            _successEffect = null;
        }

        private static void BindClick(Component target, Action callback)
        {
            if (target == null) return;
            // 转换后的 Laya Sprite 容器可能保留一张 disabled/alpha=0 的占位 Image。
            // 只找子 Image 会把按钮画出来却让容器本身永远不进 GraphicRegistry。
            // 统一走 Component 重载：启用/补齐容器透明命中体，并把点击语义挂在真实盒子上。
            UIUtil.AddClick(target, callback);
        }

        private static void SetNode(Component node, bool active)
        {
            if (node != null) node.gameObject.SetActive(active);
        }

        private static void DestroyRuntime(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }
}
