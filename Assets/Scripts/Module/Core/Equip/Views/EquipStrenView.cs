using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 天殒淬炉主页面。装备位和中心展示都只能从 EquipModule 内的专用
    /// EquipStrenItem 模板克隆；模板尚未由 Unity 保存时保持明确降级，不创建替代视觉树。
    /// </summary>
    public sealed class EquipStrenView : EquipStrenViewBind
    {
        [SerializeField] private EquipStrenItem _tpl_EquipStrenItem;

        internal EquipStrenItem ItemTemplate => _tpl_EquipStrenItem;
        internal GameObject EquipmentTemplate => _tpl_EquipmentItem;
        internal GameObject FightingTemplate => _tpl_FightingShowSmallItem;

        private readonly List<EquipStrenItem> _equipmentSlots = new List<EquipStrenItem>(10);
        private readonly List<EquipAttrItem> _attributeItems = new List<EquipAttrItem>(4);
        private readonly HashSet<int> _queriedEquipTypes = new HashSet<int>();
        private EquipStrenItem _showItem;
        private FightingShowSmallItem _fightingItem;
        private int _selectedEquipType;
        private bool _subscribed;
        private bool _missingTemplateLogged;
        private int _configEpoch;
        private bool _costIconRequested;

        protected override void OnInit()
        {
            HideRedsAndEffects();
            HideTemplates();
            SetStaticLabels();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            _selectedEquipType = 0;
            _queriedEquipTypes.Clear();
            Subscribe();
            BuildEquipmentItems();
            RefreshEquipmentItems();
            if (!BagModel.Instance.HasEquipmentData)
                EquipWearController.Instance.RequestWornList();
            _ = EnsureConfigsAndRefreshAsync(++_configEpoch);
        }

        protected override void OnHide()
        {
            EquipFlow.CloseSub("EquipStrenMasterView");
            Unsubscribe();
            ++_configEpoch;
            HideRuntimeItems();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            ++_configEpoch;
            _equipmentSlots.Clear();
            _attributeItems.Clear();
            _queriedEquipTypes.Clear();
            _showItem = null;
            _fightingItem = null;
            base.OnDispose();
        }

        public void SelectEquipType(int equipType)
        {
            if (equipType <= 0 || EquipAutoWear.GetWorn(equipType) == null) return;
            _selectedEquipType = equipType;
            RefreshSelection();
            RefreshShowItem();
            QueryStrengthIfNeeded(equipType);
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentItems);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_STREN_UPDATE, RefreshStrengthLevels);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshShowItem);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshWholeReward);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentItems);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_STREN_UPDATE, RefreshStrengthLevels);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshShowItem);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshWholeReward);
            _subscribed = false;
        }

        private void BuildEquipmentItems()
        {
            if (_tpl_EquipStrenItem == null)
            {
                if (!_missingTemplateLogged)
                {
                    GameLog.Warn("Equip", "EquipStrenView 缺少 Equip 私有 _tpl_EquipStrenItem；Strength 装备位保持 blocked");
                    _missingTemplateLogged = true;
                }
                return;
            }

            if (_equipmentSlots.Count == 0 && left_groupEquip != null && right_groupEquip != null)
            {
                for (int equipType = 1; equipType <= 10; equipType++)
                {
                    RectTransform parent = equipType % 2 == 1 ? left_groupEquip : right_groupEquip;
                    EquipStrenItem item = CreateItem(parent, "EquipStrenItem_Runtime_" + equipType);
                    if (item == null) continue;
                    item.SetEquipPos(equipType);
                    item.SetInteractive(true);
                    item.SetSelectCallback(SelectEquipType);
                    _equipmentSlots.Add(item);
                }
            }
            else
            {
                foreach (EquipStrenItem item in _equipmentSlots)
                    if (item != null && !item.IsShown) item.Show();
            }

            if (_showItem == null && gp_show != null)
            {
                _showItem = CreateItem(gp_show, "EquipStrenItem_Show_Runtime");
                if (_showItem != null)
                {
                    _showItem.SetEquipPos(0);
                    _showItem.SetInteractive(false);
                    _showItem.SetEquipmentTooltipEnabled(true);
                }
            }
            else if (_showItem != null && !_showItem.IsShown)
            {
                _showItem.Show();
            }

            EnsureFightingItem();
        }

        private EquipStrenItem CreateItem(RectTransform parent, string itemName)
        {
            GameObject go = Instantiate(_tpl_EquipStrenItem.gameObject, parent, false);
            go.name = itemName;
            go.SetActive(false);
            EquipStrenItem item = go.GetComponent<EquipStrenItem>();
            if (item == null)
            {
                Destroy(go);
                GameLog.Warn("Equip", "_tpl_EquipStrenItem 缺少 EquipStrenItem 业务组件");
                return null;
            }
            item.Show();
            return item;
        }

        private void RefreshEquipmentItems()
        {
            int firstWorn = 0;
            int bestEquipType = 0;
            int bestStrength = int.MaxValue;
            bool previousStillWorn = false;

            for (int equipType = 1; equipType <= 10; equipType++)
            {
                BagGoods worn = EquipAutoWear.GetWorn(equipType);
                if (worn == null) continue;
                if (firstWorn == 0) firstWorn = equipType;
                if (equipType == _selectedEquipType) previousStillWorn = true;

                int strength = EquipStrenModel.Instance.HasStren(equipType)
                    ? EquipStrenModel.Instance.GetStren(equipType)
                    : worn.Stren;
                if (!IsAtMax(worn, equipType) && strength < bestStrength)
                {
                    bestStrength = strength;
                    bestEquipType = equipType;
                }
                QueryStrengthIfNeeded(equipType);
            }

            foreach (EquipStrenItem item in _equipmentSlots)
            {
                BagGoods worn = EquipAutoWear.GetWorn(item.EquipType);
                item.SetData(worn, _tpl_EquipmentItem);
                item.SetMax(IsAtMax(worn, item.EquipType));
            }

            _selectedEquipType = previousStillWorn
                ? _selectedEquipType
                : (bestEquipType != 0 ? bestEquipType : firstWorn);
            RefreshSelection();
            RefreshShowItem();
        }

        private void RefreshStrengthLevels()
        {
            foreach (EquipStrenItem item in _equipmentSlots)
                item.RefreshStrengthLevel();
            RefreshShowItem();
        }

        private void RefreshSelection()
        {
            foreach (EquipStrenItem item in _equipmentSlots)
                item.SetSelect(item.EquipType == _selectedEquipType);
        }

        private void RefreshShowItem()
        {
            BagGoods worn = _selectedEquipType > 0 ? EquipAutoWear.GetWorn(_selectedEquipType) : null;
            if (_showItem != null)
            {
                _showItem.SetData(worn, _tpl_EquipmentItem);
                _showItem.SetMax(IsAtMax(worn, _selectedEquipType));
            }
            RefreshStrengthPreview(worn);
            RefreshTotalPower();
        }

        private void RefreshStrengthPreview(BagGoods worn)
        {
            bool hasEquipment = worn != null && _selectedEquipType > 0;
            SetActive(groupAttr, hasEquipment);
            SetActive(groupGoods, hasEquipment);
            if (!hasEquipment)
            {
                HideAttributeItems();
                SetActive(group_cost, false);
                SetActive(group_max_tip, false);
                return;
            }

            int level = CurrentStrength(_selectedEquipType, worn);
            bool hasCurrent = EquipConfigs.TryGetStrengthLevel(_selectedEquipType, level, out EquipConfigs.StrengthLevel current);
            bool isMax = IsAtMax(worn, _selectedEquipType);
            EquipConfigs.StrengthLevel next = default;
            bool hasNext = !isMax && EquipConfigs.TryGetStrengthLevel(_selectedEquipType, level + 1, out next);
            if (!hasCurrent)
            {
                HideAttributeItems();
                SetActive(groupAttr, false);
                SetActive(groupGoods, false);
                SetActive(group_cost, false);
                SetActive(group_max_tip, false);
                return;
            }

            SetActive(groupAttr, true);
            SetActive(groupGoods, true);
            ShowAttributeItems(current, hasNext ? next : default, level, hasNext);
            SetActive(group_max_tip, isMax);
            SetActive(group_cost, true);
            RefreshCost(current.CoinCost, isMax);
        }

        private void ShowAttributeItems(EquipConfigs.StrengthLevel current, EquipConfigs.StrengthLevel next,
            int level, bool hasNext)
        {
            int count = current.Attributes?.Count ?? 0;
            EnsureAttributeItemCount(count);
            for (int i = 0; i < _attributeItems.Count; i++)
            {
                EquipAttrItem item = _attributeItems[i];
                if (i >= count)
                {
                    if (item.IsShown) item.Hide();
                    continue;
                }

                if (!item.IsShown) item.Show();
                EquipConfigs.StrengthAttribute attrData = current.Attributes[i];
                long currentValue = attrData.PerLevelValue * level;
                long nextValue = currentValue;
                if (hasNext && TryFindAttribute(next.Attributes, attrData.AttrId, out EquipConfigs.StrengthAttribute upcoming))
                    nextValue = upcoming.PerLevelValue * (level + 1L);
                item.SetStrengthPreview(attrData.AttrId, currentValue, nextValue, hasNext);
            }
        }

        private void EnsureAttributeItemCount(int count)
        {
            if (_tpl_EquipAttrItem == null || listAttr == null) return;
            while (_attributeItems.Count < count)
            {
                GameObject go = Instantiate(_tpl_EquipAttrItem, listAttr, false);
                go.name = "EquipAttrItem_Runtime_" + (_attributeItems.Count + 1);
                go.SetActive(false);
                EquipAttrItem item = go.GetComponent<EquipAttrItem>();
                if (item == null)
                {
                    Destroy(go);
                    GameLog.Warn("Equip", "_tpl_EquipAttrItem 缺少 EquipAttrItem 业务组件");
                    return;
                }
                item.Show();
                _attributeItems.Add(item);
            }
        }

        private void EnsureFightingItem()
        {
            if (_fightingItem != null)
            {
                if (!_fightingItem.IsShown) _fightingItem.Show();
                return;
            }
            if (_tpl_FightingShowSmallItem == null || _fight_power == null) return;

            GameObject go = Instantiate(_tpl_FightingShowSmallItem, _fight_power, false);
            go.name = "FightingShowSmallItem_Runtime";
            go.SetActive(false);
            _fightingItem = go.GetComponent<FightingShowSmallItem>();
            if (_fightingItem == null)
            {
                Destroy(go);
                GameLog.Warn("Equip", "_tpl_FightingShowSmallItem 缺少 FightingShowSmallItem 业务组件");
                return;
            }
            _fightingItem.Show();
        }

        private void RefreshTotalPower()
        {
            EnsureFightingItem();
            if (_fightingItem == null) return;

            long total = 0;
            for (int equipType = 1; equipType <= 10; equipType++)
            {
                BagGoods worn = EquipAutoWear.GetWorn(equipType);
                if (worn == null) continue;
                int level = CurrentStrength(equipType, worn);
                if (EquipConfigs.TryGetStrengthLevel(equipType, level, out EquipConfigs.StrengthLevel data))
                    total += EquipConfigs.CalculateStrengthPower(data.Attributes, level);
            }
            int allLevel = TotalStrengthLevel();
            EquipConfigs.GetWholeRewardPair(1, allLevel, out bool hasMaster, out EquipConfigs.WholeReward master,
                out _, out _);
            if (hasMaster) total += EquipConfigs.CalculateStrengthPower(master.Attributes, 1);
            _fightingItem.SetFighting(total);
            _fightingItem.SetFightingUp(0);
        }

        private void RefreshCost(long need, bool isMax)
        {
            long have = RoleModel.Instance.Coin;
            if (have_num != null)
            {
                have_num.text = isMax ? "已满级" : FormatCoin(have);
                have_num.color = isMax || have >= need
                    ? new Color32(10, 149, 62, 255)
                    : new Color32(255, 79, 80, 255);
            }
            if (need_num != null) need_num.text = isMax ? string.Empty : "/" + FormatCoin(need);
            EnsureCostIcon();
        }

        private void EnsureCostIcon()
        {
            if (_costIconRequested || goods_icon == null) return;
            string icon = GoodsModel.GetGoodsIcon(31);
            if (string.IsNullOrEmpty(icon)) return;
            _costIconRequested = true;
            goods_icon.gameObject.SetActive(true);
            _ = ResManager.SetImageAsync(goods_icon, GameResPath.GetGoodsIconPath(icon), false, false);
        }

        private static int CurrentStrength(int equipType, BagGoods worn)
        {
            return EquipStrenModel.Instance.HasStren(equipType)
                ? EquipStrenModel.Instance.GetStren(equipType)
                : Mathf.Max(0, worn?.Stren ?? 0);
        }

        internal static int TotalStrengthLevel()
        {
            int total = 0;
            for (int equipType = 1; equipType <= 10; equipType++)
            {
                BagGoods worn = EquipAutoWear.GetWorn(equipType);
                if (worn != null) total += CurrentStrength(equipType, worn);
            }
            return total;
        }

        private void RefreshWholeReward()
        {
            int activated = EquipWholeAwardModel.Instance.GetWholeLv(1);
            if (grade != null) grade.text = activated > 0 ? ("Lv." + activated) : string.Empty;
            bool ready = EquipConfigs.TryGetNextWholeReward(1, activated, out EquipConfigs.WholeReward next)
                && TotalStrengthLevel() >= next.NeedLevel;
            SetActive(red_master, ready);
            RefreshTotalPower();
        }

        private static bool TryFindAttribute(IReadOnlyList<EquipConfigs.StrengthAttribute> attributes, int attrId,
            out EquipConfigs.StrengthAttribute value)
        {
            if (attributes != null)
            {
                for (int i = 0; i < attributes.Count; i++)
                {
                    if (attributes[i].AttrId != attrId) continue;
                    value = attributes[i];
                    return true;
                }
            }
            value = default;
            return false;
        }

        private static string FormatCoin(long value)
        {
            if (value > 10000 && value < 100000000)
            {
                string text = (value / 10000d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                if (text.EndsWith(".0")) text = text.Substring(0, text.Length - 2);
                return text + "万";
            }
            if (value >= 100000000)
            {
                string text = (value / 100000000d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                if (text.EndsWith(".0")) text = text.Substring(0, text.Length - 2);
                return text + "亿";
            }
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private async Task EnsureConfigsAndRefreshAsync(int epoch)
        {
            await EquipConfigs.EnsureLoaded();
            if (this == null || epoch != _configEpoch || !IsShown) return;
            RefreshEquipmentItems();
            RefreshWholeReward();
        }

        private static bool IsAtMax(BagGoods worn, int equipType)
        {
            if (worn == null || equipType <= 0) return false;
            GoodsModel.EquipAttr equip = GoodsModel.GetEquipAttr(worn.TypeId);
            if (equip == null ||
                !EquipConfigs.TryGetStrengthenMax(equip.Stage, worn.Color, equipType, out int maxLevel))
                return false;
            int strength = EquipStrenModel.Instance.HasStren(equipType)
                ? EquipStrenModel.Instance.GetStren(equipType)
                : worn.Stren;
            return strength >= maxLevel;
        }

        private static bool AreAllWornAtMax()
        {
            bool found = false;
            for (int equipType = 1; equipType <= 10; equipType++)
            {
                BagGoods worn = EquipAutoWear.GetWorn(equipType);
                if (worn == null) continue;
                found = true;
                if (!IsAtMax(worn, equipType)) return false;
            }
            return found;
        }

        private void QueryStrengthIfNeeded(int equipType)
        {
            if (equipType <= 0 || EquipStrenModel.Instance.HasStren(equipType) || !_queriedEquipTypes.Add(equipType))
                return;
            EquipStrenController.Instance.QueryStren(equipType);
        }

        private void HideRuntimeItems()
        {
            foreach (EquipStrenItem item in _equipmentSlots)
                if (item != null && item.IsShown) item.Hide();
            if (_showItem != null && _showItem.IsShown) _showItem.Hide();
            HideAttributeItems();
            if (_fightingItem != null && _fightingItem.IsShown) _fightingItem.Hide();
        }

        private void HideAttributeItems()
        {
            foreach (EquipAttrItem item in _attributeItems)
                if (item != null && item.IsShown) item.Hide();
        }

        private void HideRedsAndEffects()
        {
            SetActive(red_master, false);
            SetActive(_reddot, false);
            SetActive(gp_effect, false);
        }

        private void HideTemplates()
        {
            if (_tpl_EquipStrenItem != null) _tpl_EquipStrenItem.gameObject.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_EquipAttrItem != null) _tpl_EquipAttrItem.SetActive(false);
        }

        private void SetStaticLabels()
        {
            if (_Label2 != null) _Label2.text = "天殒淬炉消耗";
            if (_lb_strOne != null) _lb_strOne.text = "强化";
            if (_lb_strAll != null) _lb_strAll.text = "一键强化";
        }

        private void BindButtons()
        {
            BindClick(btnStrOne, () =>
            {
                if (!RequireSelectedEquipment()) return;
                if (IsAtMax(EquipAutoWear.GetWorn(_selectedEquipType), _selectedEquipType))
                {
                    TipsManager.Toast("达到天殒淬炉上限");
                    return;
                }
                if (!CanAffordSelectedStrength()) return;
                EquipStrenController.Instance.StrenOne(_selectedEquipType);
            });
            BindClick(btnStrAll, () =>
            {
                if (!RequireSelectedEquipment()) return;
                if (AreAllWornAtMax())
                {
                    TipsManager.Toast("达到天殒淬炉上限");
                    return;
                }
                if (!CanAffordSelectedStrength()) return;
                EquipStrenController.Instance.StrenAll();
            });
            BindClick(btnStopAll, () =>
                GameLog.Info("Equip", "天殒淬炉停止按钮沿用老端当前空操作语义"));
            BindClick(btnMaster, () => EquipFlow.OpenSub("EquipStrenMasterView"));
        }

        private bool RequireSelectedEquipment()
        {
            if (_selectedEquipType > 0 && EquipAutoWear.GetWorn(_selectedEquipType) != null) return true;
            TipsManager.Toast("需要穿戴装备才可以天殒淬炉");
            return false;
        }

        private bool CanAffordSelectedStrength()
        {
            BagGoods worn = EquipAutoWear.GetWorn(_selectedEquipType);
            if (worn == null) return false;
            int level = CurrentStrength(_selectedEquipType, worn);
            if (!EquipConfigs.TryGetStrengthLevel(_selectedEquipType, level, out EquipConfigs.StrengthLevel data))
            {
                TipsManager.Toast("强化配置未就绪");
                return false;
            }
            if (RoleModel.Instance.Coin >= data.CoinCost) return true;
            TipsManager.Toast("铜币不足");
            return false;
        }

        private static void BindClick(Component target, System.Action callback)
        {
            if (target == null) return;
            Image image = target as Image;
            if (image == null) image = target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, callback);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }
    }
}
