using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 神兵淬炼(精炼)界面(对标老客户端 equip/EquipSmeltView.ts):左右两列穿戴装备格(left/right_groupEquip 内
    /// EquipSmeltItem)+ 选中展示位(gp_show)+ 战力(_fight_power FightingShowSmallItem)+ 属性列表(listAttr
    /// EquipAttrItem)+ 消耗材料(goods_icon/goods_icon_top BaseAwardItem,标题"神兵淬炼消耗")+ 精炼/一键精炼
    /// 按钮(btnStrOne/btnStrAll)+ 红点(_reddot)+ 各特效层(_group_eff*/gp_effect)。
    ///
    /// 精炼协议(15250/15251,经 EquipSmeltController,自动循环 轮4 队列#4)已接线；单件/一键精炼都必须先由
    /// 真实装备格建立选中态；当前已从既有 Prefab 模板按奇偶位克隆 10 槽，消费 15010 穿戴快照，
    /// 并按需查询 15250 淬炼等级。EquipSmeltItem 选中态由 <see cref="SelectEquipType"/> 统一互斥刷新。
    /// 降级:config_equip_refine_*、属性/双材料/战力/红点与淬炼特效资源闭包尚未完成，相关模板先隐藏。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipSmeltView : EquipSmeltViewBind
    {
        /// <summary>当前选中部位(对标老端 SELECT_SMELT_EQUIP)，默认取第一件真实穿戴装备。</summary>
        private int _selectedEquipType;
        private readonly List<EquipSmeltItem> _equipmentSlots = new List<EquipSmeltItem>(10);
        private EquipSmeltItem _showItem;
        private EquipAttrItem _ratioItem;
        private BaseAwardItem _materialItem;
        private FightingShowSmallItem _fightingItem;
        private bool _subscribed;
        private int _epoch;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            SetStaticLabels();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            _selectedEquipType = 0;
            ++_epoch;
            Subscribe();
            BuildEquipmentSlots();
            RefreshEquipmentSlots();
            if (!BagModel.Instance.HasEquipmentData)
                EquipWearController.Instance.RequestWornList();
            _ = EnsureConfigAndRefreshAsync(_epoch);
        }

        protected override void OnHide()
        {
            ++_epoch;
            Unsubscribe();
            foreach (EquipSmeltItem item in _equipmentSlots)
                if (item != null && item.IsShown) item.Hide();
            if (_showItem != null && _showItem.IsShown) _showItem.Hide();
            if (_ratioItem != null && _ratioItem.IsShown) _ratioItem.Hide();
            if (_materialItem != null && _materialItem.IsShown) _materialItem.Hide();
            if (_fightingItem != null && _fightingItem.IsShown) _fightingItem.Hide();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            _equipmentSlots.Clear();
            _showItem = null;
            _ratioItem = null;
            _materialItem = null;
            _fightingItem = null;
            base.OnDispose();
        }

        /// <summary>更新当前选中部位(由 EquipSmeltItem 点击回调驱动,对标 SELECT_SMELT_EQUIP)。</summary>
        public void SelectEquipType(int equipType)
        {
            if (equipType == 0) return;
            _selectedEquipType = equipType;
            foreach (EquipSmeltItem item in _equipmentSlots)
                item.SetSelect(item.EquipType == equipType);
            EquipSmeltController.Instance.QuerySmelt(equipType);
            RefreshSelectedDetails();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentSlots);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_SMELT_UPDATE, RefreshSmeltLevels);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshSelectedDetails);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_STREN_UPDATE, RefreshSelectedDetails);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshSelectedDetails);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentSlots);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_SMELT_UPDATE, RefreshSmeltLevels);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshSelectedDetails);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_STREN_UPDATE, RefreshSelectedDetails);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshSelectedDetails);
            _subscribed = false;
        }

        private void BuildEquipmentSlots()
        {
            if (_equipmentSlots.Count > 0)
            {
                foreach (EquipSmeltItem item in _equipmentSlots)
                    if (item != null && !item.IsShown) item.Show();
                return;
            }
            if (_tpl_EquipSmeltItem == null || left_groupEquip == null || right_groupEquip == null) return;

            for (int equipType = 1; equipType <= 10; equipType++)
            {
                // 老端按 EQUIP_POS 顺序把奇数位放左列、偶数位放右列。
                RectTransform parent = equipType % 2 == 1 ? left_groupEquip : right_groupEquip;
                GameObject go = Instantiate(_tpl_EquipSmeltItem, parent, false);
                go.name = "EquipSmeltItem_Runtime_" + equipType;
                go.SetActive(false);
                EquipSmeltItem item = go.GetComponent<EquipSmeltItem>();
                if (item == null)
                {
                    Destroy(go);
                    GameLog.Warn("Equip", "_tpl_EquipSmeltItem 缺 EquipSmeltItem 业务组件 equip_type={0}", equipType);
                    continue;
                }
                item.Show();
                item.SetEquipPos(equipType);
                item.SetSelectCallback(SelectEquipType);
                _equipmentSlots.Add(item);
            }
            BuildDetailItems();
        }

        private void BuildDetailItems()
        {
            if (_showItem == null && _tpl_EquipSmeltItem != null && gp_show != null)
            {
                GameObject go = Instantiate(_tpl_EquipSmeltItem, gp_show, false);
                go.name = "EquipSmeltItem_Show_Runtime";
                go.SetActive(false);
                _showItem = go.GetComponent<EquipSmeltItem>();
                if (_showItem != null)
                {
                    _showItem.Show();
                    _showItem.SetEquipPos(0);
                    _showItem.SetInteractive(false, true);
                }
                else Destroy(go);
            }
            else if (_showItem != null && !_showItem.IsShown) _showItem.Show();

            if (_ratioItem == null && _tpl_EquipAttrItem != null && listAttr != null)
            {
                GameObject go = Instantiate(_tpl_EquipAttrItem, listAttr, false);
                go.name = "EquipAttrItem_SmeltRatio_Runtime";
                go.SetActive(false);
                _ratioItem = go.GetComponent<EquipAttrItem>();
                if (_ratioItem != null) _ratioItem.Show(); else Destroy(go);
            }
            else if (_ratioItem != null && !_ratioItem.IsShown) _ratioItem.Show();

            if (_materialItem == null && _tpl_BaseAwardItem != null && goods_icon != null)
            {
                GameObject go = Instantiate(_tpl_BaseAwardItem, goods_icon, false);
                go.name = "BaseAwardItem_SmeltMaterial_Runtime";
                go.SetActive(false);
                _materialItem = go.GetComponent<BaseAwardItem>();
                if (_materialItem != null)
                {
                    _materialItem.Show();
                    _materialItem.SetScale(70f / 127f);
                }
                else Destroy(go);
            }
            else if (_materialItem != null && !_materialItem.IsShown) _materialItem.Show();

            if (_fightingItem == null && _tpl_FightingShowSmallItem != null && _fight_power != null)
            {
                GameObject go = Instantiate(_tpl_FightingShowSmallItem, _fight_power, false);
                go.name = "FightingShowSmallItem_Runtime";
                go.SetActive(false);
                _fightingItem = go.GetComponent<FightingShowSmallItem>();
                if (_fightingItem != null) _fightingItem.Show(); else Destroy(go);
            }
            else if (_fightingItem != null && !_fightingItem.IsShown) _fightingItem.Show();
        }

        private void RefreshEquipmentSlots()
        {
            int previous = _selectedEquipType;
            int firstWorn = 0;
            bool previousStillWorn = false;

            foreach (EquipSmeltItem item in _equipmentSlots)
            {
                BagGoods worn = EquipAutoWear.GetWorn(item.EquipType);
                if (worn != null)
                {
                    if (firstWorn == 0) firstWorn = item.EquipType;
                    if (item.EquipType == previous) previousStillWorn = true;
                    if (!EquipSmeltModel.Instance.HasSmelt(item.EquipType))
                        EquipSmeltController.Instance.QuerySmelt(item.EquipType);
                }
                item.SetData(worn, _tpl_EquipmentItem);
                item.SetMax(IsAtMax(worn, item.EquipType));
            }

            _selectedEquipType = previousStillWorn ? previous : firstWorn;
            foreach (EquipSmeltItem item in _equipmentSlots)
                item.SetSelect(item.EquipType == _selectedEquipType);
            RefreshSelectedDetails();
        }

        private void RefreshSmeltLevels()
        {
            foreach (EquipSmeltItem item in _equipmentSlots)
                item.RefreshSmeltLevel();
            RefreshEquipmentSlots();
        }

        private async Task EnsureConfigAndRefreshAsync(int epoch)
        {
            await EquipConfigs.EnsureLoaded();
            if (this == null || !IsShown || epoch != _epoch) return;
            RefreshEquipmentSlots();
        }

        private void RefreshSelectedDetails()
        {
            BuildDetailItems();
            BagGoods worn = _selectedEquipType > 0 ? EquipAutoWear.GetWorn(_selectedEquipType) : null;
            if (_showItem != null)
            {
                _showItem.SetEquipPos(_selectedEquipType);
                _showItem.SetInteractive(false, true);
                _showItem.SetData(worn, _tpl_EquipmentItem);
                _showItem.SetMax(IsAtMax(worn, _selectedEquipType));
            }
            bool visible = worn != null;
            if (groupAttr != null) groupAttr.gameObject.SetActive(visible);
            if (groupGoods != null) groupGoods.gameObject.SetActive(visible);
            if (!visible)
            {
                if (_ratioItem != null && _ratioItem.IsShown) _ratioItem.Hide();
                if (_materialItem != null && _materialItem.IsShown) _materialItem.Hide();
                if (goods_name != null) goods_name.text = string.Empty;
                if (goods_num != null) goods_num.text = string.Empty;
                if (group_cost != null) group_cost.gameObject.SetActive(false);
                RefreshPower();
                return;
            }

            if (_ratioItem != null && !_ratioItem.IsShown) _ratioItem.Show();
            (int refine, _) = EquipSmeltModel.Instance.GetSmelt(_selectedEquipType);
            int cumulative = EquipConfigs.GetCumulativeSmeltRatio(_selectedEquipType, refine);
            int nextRatio = EquipConfigs.TryGetSmeltLevel(_selectedEquipType, refine, out EquipConfigs.SmeltLevel current)
                ? current.StrengthRatio : 0;
            if (_ratioItem != null) _ratioItem.SetSmeltPreview(_selectedEquipType, cumulative, IsAtMax(worn, _selectedEquipType) ? 0 : nextRatio);

            int costLevel = refine == 0 ? 0 : refine + 1;
            bool atMax = IsAtMax(worn, _selectedEquipType);
            EquipConfigs.SmeltLevel cost = default;
            bool hasCost = !atMax
                && EquipConfigs.TryGetSmeltLevel(_selectedEquipType, costLevel, out cost);
            if (group_cost != null) group_cost.gameObject.SetActive(true);
            if (need_gp != null) need_gp.gameObject.SetActive(true);
            if (need_top_gp != null) need_top_gp.gameObject.SetActive(false);
            if (hasCost)
            {
                if (_materialItem != null && !_materialItem.IsShown) _materialItem.Show();
                long own = BagModel.Instance.GetTypeGoodsNum(cost.MaterialTypeId);
                if (_materialItem != null) _materialItem.SetData(cost.MaterialTypeId, 1);
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(cost.MaterialTypeId);
                if (goods_name != null) goods_name.text = basic?.Name ?? string.Empty;
                if (goods_num != null)
                {
                    goods_num.text = own + "/" + cost.NeedNum;
                    goods_num.color = own >= cost.NeedNum
                        ? new Color32(10, 149, 62, 255)
                        : new Color32(255, 79, 80, 255);
                }
            }
            else
            {
                if (_materialItem != null && _materialItem.IsShown) _materialItem.Hide();
                if (goods_name != null) goods_name.text = string.Empty;
                if (goods_num != null) goods_num.text = atMax ? "已满级" : "配置未就绪";
            }
            RefreshPower();
        }

        private void RefreshPower()
        {
            if (_fightingItem == null) return;
            int totalRefine = 0;
            for (int type = 1; type <= 10; type++) totalRefine += EquipSmeltModel.Instance.GetSmelt(type).refine;
            EquipConfigs.GetWholeRewardPair(8, totalRefine, out bool hasMaster, out EquipConfigs.WholeReward master,
                out _, out _);
            int masterRatio = hasMaster ? master.StrengthRatio : 0;
            long totalPower = 0;
            for (int type = 1; type <= 10; type++)
            {
                BagGoods worn = EquipAutoWear.GetWorn(type);
                int refine = EquipSmeltModel.Instance.GetSmelt(type).refine;
                if (worn == null || refine <= 0) continue;
                int strength = EquipStrenModel.Instance.HasStren(type) ? EquipStrenModel.Instance.GetStren(type) : worn.Stren;
                if (!EquipConfigs.TryGetStrengthLevel(type, 0, out EquipConfigs.StrengthLevel baseStrength)) continue;
                int ratio = masterRatio;
                for (int i = 0; i <= refine; i++)
                    if (EquipConfigs.TryGetSmeltLevel(type, i, out EquipConfigs.SmeltLevel row)) ratio += row.StrengthRatio;
                var scaled = new List<EquipConfigs.StrengthAttribute>(baseStrength.Attributes.Count);
                foreach (EquipConfigs.StrengthAttribute attr in baseStrength.Attributes)
                    scaled.Add(new EquipConfigs.StrengthAttribute(attr.AttrId,
                        (long)System.Math.Floor(attr.PerLevelValue * strength * ratio / 10000d)));
                totalPower += EquipConfigs.CalculateStrengthPower(scaled, 1);
            }
            _fightingItem.SetFighting(totalPower);
            _fightingItem.SetFightingUp(0);
        }

        private static bool IsAtMax(BagGoods worn, int equipType)
        {
            if (worn == null || equipType <= 0) return false;
            GoodsModel.EquipAttr equip = GoodsModel.GetEquipAttr(worn.TypeId);
            return equip != null
                && EquipConfigs.TryGetRefineMax(equip.Stage, worn.Color, equipType, out int max)
                && EquipSmeltModel.Instance.GetSmelt(equipType).refine >= max;
        }

        private bool CanAffordSelected()
        {
            BagGoods worn = EquipAutoWear.GetWorn(_selectedEquipType);
            if (worn == null || IsAtMax(worn, _selectedEquipType)) return false;
            int refine = EquipSmeltModel.Instance.GetSmelt(_selectedEquipType).refine;
            int costLevel = refine == 0 ? 0 : refine + 1;
            if (!EquipConfigs.TryGetSmeltLevel(_selectedEquipType, costLevel, out EquipConfigs.SmeltLevel cost))
            {
                TipsManager.Toast("淬炼配置未就绪");
                return false;
            }
            if (BagModel.Instance.GetTypeGoodsNum(cost.MaterialTypeId) >= cost.NeedNum) return true;
            TipsManager.Toast("淬炼材料不足");
            return false;
        }

        private void HideReds()
        {
            HideNode(_reddot);
        }

        private void HideTemplates()
        {
            if (_tpl_EquipSmeltItem != null) _tpl_EquipSmeltItem.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_EquipAttrItem != null) _tpl_EquipAttrItem.SetActive(false);
        }

        /// <summary>静态文案(对标老端 LoadSuccess 里固定赋值的标题/按钮文字)。</summary>
        private void SetStaticLabels()
        {
            if (_Label2 != null) _Label2.text = "神兵淬炼消耗";
            if (_lb_strOne != null) _lb_strOne.text = "精炼";
            if (_lb_strAll != null) _lb_strAll.text = "一键精炼";
        }

        private void BindButtons()
        {
            BindClick(btnStrOne, () =>
            {
                if (_selectedEquipType <= 0)
                {
                    TipsManager.Toast("请先选择已穿戴装备");
                    GameLog.Warn("Equip", "点击[精炼]被阻止：装备列表尚未建立真实选中态");
                    return;
                }
                if (!CanAffordSelected()) return;
                GameLog.Info("Equip", "点击[精炼] → SmeltOne(equip_type={0})", _selectedEquipType);
                EquipSmeltController.Instance.SmeltOne(_selectedEquipType);
            });
            BindClick(btnStrAll, () =>
            {
                if (_selectedEquipType <= 0)
                {
                    TipsManager.Toast("请先选择已穿戴装备");
                    GameLog.Warn("Equip", "点击[一键精炼]被阻止：装备列表尚未建立真实选中态");
                    return;
                }
                GameLog.Info("Equip", "点击[一键精炼] → SmeltAll(equip_type={0})", _selectedEquipType);
                EquipSmeltController.Instance.SmeltAll(_selectedEquipType);
            });
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击回调。</summary>
        private void BindClick(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
