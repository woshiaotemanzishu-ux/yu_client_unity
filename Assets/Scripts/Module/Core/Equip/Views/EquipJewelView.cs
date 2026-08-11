using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Jewel;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 骸珀镶嵌主页签(对标老客户端 jewel/EquipJewelView.ts):装备位列表(left/right_groupEquip,EquipJewelPosItem)+
    /// 当前选中件的镶嵌槽预览(pos_1..pos_6,EquipJewelItem)+ 战力(_fight_power)+ 一键镶嵌/升级(btnStrAll)+
    /// 淬炉宗师式大师入口(btnMaster→骸珀镶嵌大师,type=3)+ 雕刻入口(_img_crave→星骸凿痕子窗)。
    ///
    /// **装备位列表/镶嵌槽渲染降级说明**:老端两者都是运行时 new 出来克隆同一个 LayaUI 资源(装备位列表借用
    /// "equip/EquipStrenItem" 布局,即 <see cref="Shenxiao.Generated.UI.Equip.EquipStrenItemBind"/>;镶嵌槽是
    /// <see cref="EquipJewelItem"/>)。核查当前烤图(JewelModule.prefab):left/right_groupEquip 与 pos_1..pos_6
    /// 均为**空容器**。当前 JewelModule.prefab 的隐藏 `__Templates` 下已有 EquipJewelItem，但 Bind/View 尚未
    /// 持有并克隆该模板；装备位模板仍未落地(同类空缺在 EquipStrenView 也未处理)。因此本页当前仍不臆造
    /// 运行时节点树，后续应在既有 Prefab 上增量补序列化引用与克隆，并沿真实滚动/点击路径复验。
    ///
    /// btnStrAll 当前仅保留"一键升级"语义(老端另有"一键镶嵌"模式,需 config_equip_stone_inlay 自动选材)；
    /// 一键升级按 config_equip_stone_lv 的等级/槽位顺序串行发送 15215 upgrade_type=0，
    /// 每次必须等待权威回包并刷新动态宝石 type_id 后才能继续，禁止 type=1 或并发扫发。
    /// 装备位列表未落地时保持无选中态并阻断 15215，禁止固定读取武器槽。
    /// </summary>
    public sealed class EquipJewelView : EquipJewelViewBind
    {
        /// <summary>骸珀镶嵌大师(全身奖励)type(对标老端 EquipDefine.JEWEL_WHOLE_TYPE=3)。</summary>
        private const int JewelWholeType = 3;

        private readonly List<EquipStrenItem> _equipmentSlots = new List<EquipStrenItem>(10);
        private readonly List<EquipJewelItem> _jewelSlots = new List<EquipJewelItem>(6);
        private int _selectedEquipType;
        private long _selectedGoodsId;
        private GameObject _equipmentItemTemplate;
        private FightingShowSmallItem _fightingItem;
        private bool _subscribed;
        private bool _templateWarningLogged;
        private bool _oneKeyUpgradeRunning;
        private bool _upgradeRequestPending;
        private int _lifecycleEpoch;

        protected override void OnInit()
        {
            HideReds();
            HideEffects();
            if (lb_strAll != null) lb_strAll.text = "一键升级";

            BindClick(btnMaster, () =>
            {
                GameLog.Info("Equip", "点击[骸珀镶嵌大师] → OpenSub(EquipJewelMasterView, type={0})", JewelWholeType);
                EquipFlow.OpenSub("EquipJewelMasterView");
            });
            BindClick(_img_crave, () =>
            {
                GameLog.Info("Equip", "点击[雕刻入口] → OpenSub(EquipJewelCraveView)");
                EquipFlow.OpenSub("EquipJewelCraveView");
            });
            BindClick(btnStrAll, OnClickUpgradeAll);
        }

        protected override void OnShow(object args)
        {
            _oneKeyUpgradeRunning = false;
            _upgradeRequestPending = false;
            _lifecycleEpoch++;
            if (_img_crave != null)
                _img_crave.gameObject.SetActive(FuncOpenConfig.CheckFuncOpenState("EquipJewelCraveEnterView"));
            Subscribe();
            EquipJewelController.Instance.StoneUpgradeCompleted += OnStoneUpgradeCompleted;
            _ = EquipConfigs.EnsureStoneLevelLoaded();
            BuildRuntimeItems();
            RefreshEquipmentRows();
            EquipJewelController.Instance.RequestSubModPower();
            RefreshGrade();
            RefreshPower();
            if (!BagModel.Instance.HasEquipmentData) EquipWearController.Instance.RequestWornList();
        }

        /// <summary>由真实装备位列表点击建立选择；列表尚未落地时不得退化到固定武器槽。</summary>
        public void SelectEquipment(int equipType, long goodsId)
        {
            _selectedEquipType = equipType > 0 && goodsId > 0 ? equipType : 0;
            _selectedGoodsId = _selectedEquipType > 0 ? goodsId : 0;
            RefreshEquipmentSelection();
            RefreshJewelSlots();
        }

        protected override void OnHide()
        {
            _lifecycleEpoch++;
            _oneKeyUpgradeRunning = false;
            _upgradeRequestPending = false;
            EquipFlow.CloseSub("EquipJewelMasterView");
            EquipFlow.CloseSub("EquipJewelBagView");
            EquipFlow.CloseSub("EquipJewelCraveView");
            Unsubscribe();
            EquipJewelController.Instance.StoneUpgradeCompleted -= OnStoneUpgradeCompleted;
            HideRuntimeItems();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            EquipJewelController.Instance.StoneUpgradeCompleted -= OnStoneUpgradeCompleted;
            _equipmentSlots.Clear();
            _jewelSlots.Clear();
            _equipmentItemTemplate = null;
            _fightingItem = null;
            base.OnDispose();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentRows);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshEquipmentRows);
            EventDispatcher.On<long>(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, OnGoodsDetailUpdated);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_JEWEL_UPDATE, OnJewelUpdated);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshGrade);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentRows);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshEquipmentRows);
            EventDispatcher.Off<long>(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, OnGoodsDetailUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_JEWEL_UPDATE, OnJewelUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshGrade);
            _subscribed = false;
        }

        private void BuildRuntimeItems()
        {
            EquipFlow.TryGetStrengthTemplates(out EquipStrenItem equipmentTemplate,
                out _equipmentItemTemplate, out GameObject fightingTemplate);
            EquipJewelItem jewelTemplate = EquipFlow.GetJewelItemTemplate();
            if ((equipmentTemplate == null || _equipmentItemTemplate == null || jewelTemplate == null) && !_templateWarningLogged)
            {
                _templateWarningLogged = true;
                GameLog.Warn("Equip", "EquipJewelView 缺 EquipStrenItem/EquipmentItem/EquipJewelItem 已序列化模板");
            }

            if (_equipmentSlots.Count == 0 && equipmentTemplate != null && _equipmentItemTemplate != null)
            {
                for (int equipType = 1; equipType <= 10; equipType++)
                {
                    RectTransform parent = equipType % 2 == 1 ? left_groupEquip : right_groupEquip;
                    if (parent == null) continue;
                    GameObject go = Instantiate(equipmentTemplate.gameObject, parent, false);
                    go.name = "EquipJewelPosItem_Runtime_" + equipType;
                    go.SetActive(false);
                    EquipStrenItem item = go.GetComponent<EquipStrenItem>();
                    if (item == null) { Destroy(go); continue; }
                    item.Show();
                    item.SetEquipPos(equipType);
                    item.SetInteractive(true);
                    item.SetSelectCallback(type =>
                    {
                        BagGoods worn = EquipAutoWear.GetWorn(type);
                        SelectEquipment(type, worn?.GoodsId ?? 0);
                    });
                    _equipmentSlots.Add(item);
                }
            }
            else
            {
                foreach (EquipStrenItem item in _equipmentSlots)
                    if (item != null && !item.IsShown) item.Show();
            }

            if (_jewelSlots.Count == 0 && jewelTemplate != null)
            {
                RectTransform[] parents = { pos_1, pos_2, pos_3, pos_4, pos_5, pos_6 };
                for (int i = 0; i < parents.Length; i++)
                {
                    if (parents[i] == null) continue;
                    GameObject go = Instantiate(jewelTemplate.gameObject, parents[i], false);
                    go.name = "EquipJewelItem_Runtime_" + (i + 1);
                    go.SetActive(false);
                    EquipJewelItem item = go.GetComponent<EquipJewelItem>();
                    if (item == null) { Destroy(go); continue; }
                    item.Show();
                    item.SetJewelPos(i + 1);
                    _jewelSlots.Add(item);
                }
            }
            else
            {
                foreach (EquipJewelItem item in _jewelSlots)
                    if (item != null && !item.IsShown) item.Show();
            }

            if (_fightingItem == null && fightingTemplate != null && _fight_power != null)
            {
                GameObject go = Instantiate(fightingTemplate, _fight_power, false);
                go.name = "FightingShowSmallItem_Runtime";
                go.SetActive(false);
                _fightingItem = go.GetComponent<FightingShowSmallItem>();
                if (_fightingItem != null) _fightingItem.Show();
                else Destroy(go);
            }
            else if (_fightingItem != null && !_fightingItem.IsShown) _fightingItem.Show();
        }

        private void RefreshEquipmentRows()
        {
            BuildRuntimeItems();
            int firstWorn = 0;
            bool selectedStillWorn = false;
            for (int equipType = 1; equipType <= 10; equipType++)
            {
                BagGoods worn = EquipAutoWear.GetWorn(equipType);
                if (worn == null) continue;
                if (firstWorn == 0) firstWorn = equipType;
                if (equipType == _selectedEquipType && worn.GoodsId == _selectedGoodsId) selectedStillWorn = true;
            }

            foreach (EquipStrenItem item in _equipmentSlots)
            {
                BagGoods worn = EquipAutoWear.GetWorn(item.EquipType);
                item.SetData(worn, _equipmentItemTemplate);
                item.SetMax(false);
            }

            if (!selectedStillWorn)
            {
                BagGoods first = firstWorn > 0 ? EquipAutoWear.GetWorn(firstWorn) : null;
                _selectedEquipType = first != null ? firstWorn : 0;
                _selectedGoodsId = first?.GoodsId ?? 0;
            }
            RefreshEquipmentSelection();
            RefreshJewelSlots();
        }

        private void RefreshEquipmentSelection()
        {
            foreach (EquipStrenItem item in _equipmentSlots)
                item.SetSelect(item.EquipType == _selectedEquipType);
        }

        private void RefreshJewelSlots()
        {
            GoodsDetailVo detail = _selectedGoodsId > 0 ? GoodsDynamicModel.Instance.Peek(_selectedGoodsId) : null;
            foreach (EquipJewelItem item in _jewelSlots)
            {
                item.SetEquipPos(_selectedEquipType);
                item.SetEmpty();
            }
            if (_selectedGoodsId <= 0) return;
            if (detail == null)
            {
                int epoch = _lifecycleEpoch;
                long goodsId = _selectedGoodsId;
                GoodsDynamicModel.Instance.RequestDetail(goodsId, vo =>
                {
                    if (this == null || !IsShown || epoch != _lifecycleEpoch || goodsId != _selectedGoodsId) return;
                    ApplyStoneData(vo);
                });
                return;
            }
            ApplyStoneData(detail);
        }

        private void ApplyStoneData(GoodsDetailVo detail)
        {
            if (detail?.StoneList == null || detail.GoodsId != _selectedGoodsId) return;
            foreach (GoodsStoneSlot stone in detail.StoneList)
            {
                if (stone.Pos <= 0 || stone.Pos > _jewelSlots.Count) continue;
                EquipJewelItem item = _jewelSlots[stone.Pos - 1];
                if (stone.TypeId > 0) item.SetData(stone.TypeId);
                else item.SetEmpty();
            }
        }

        private void OnGoodsDetailUpdated(long goodsId)
        {
            if (goodsId == _selectedGoodsId) RefreshJewelSlots();
        }

        private void OnJewelUpdated()
        {
            RefreshJewelSlots();
            RefreshPower();
        }

        private void RefreshPower()
        {
            if (_fightingItem == null) return;
            _fightingItem.SetFighting(EquipJewelModel.Instance.GetSubModPower(1));
            _fightingItem.SetFightingUp(0);
        }

        private void HideRuntimeItems()
        {
            foreach (EquipStrenItem item in _equipmentSlots)
                if (item != null && item.IsShown) item.Hide();
            foreach (EquipJewelItem item in _jewelSlots)
                if (item != null && item.IsShown) item.Hide();
            if (_fightingItem != null && _fightingItem.IsShown) _fightingItem.Hide();
        }

        /// <summary>大师阶数展示(对标 SetGradeData):config_equip_whole_reward(lv→"阶"文案)未移植 →
        /// 直显 EquipWholeAwardModel 里的真实 whole_lv 数值,不臆造"阶"文案换算。</summary>
        private void RefreshGrade()
        {
            int lv = EquipWholeAwardModel.Instance.GetWholeLv(JewelWholeType);
            if (grade != null) grade.text = lv > 0 ? ("Lv." + lv) : "";
        }

        private void HideReds()
        {
            // red_dot(一键镶嵌/升级红点)/img_redMaster(大师红点)/crave_red(雕刻红点)均依赖 EquipModel 计算
            // (CheckEquipJewelRedAllChange/CheckMasterRed/RedDotManager JEWEL_CRAVE_RED),未移植 → 隐藏。
            HideNode(red_dot);
            HideNode(img_redMaster);
            HideNode(crave_red);
        }

        private void HideEffects()
        {
            HideNode(_group_eff);
        }

        /// <summary>btnStrAll → 一键升级。必须先由真实装备位点击建立选择。</summary>
        private void OnClickUpgradeAll()
        {
            if (_oneKeyUpgradeRunning)
            {
                TipsManager.Toast("一键升级中，请稍候");
                return;
            }
            if (_selectedEquipType <= 0 || _selectedGoodsId <= 0)
            {
                TipsManager.Toast("请先选择已穿戴装备");
                GameLog.Warn("Equip", "点击[一键升级]被阻止：装备位列表尚未建立真实选中态");
                return;
            }

            _oneKeyUpgradeRunning = true;
            _ = BeginOneKeyUpgradeAsync(_lifecycleEpoch);
        }

        private async Task BeginOneKeyUpgradeAsync(int epoch)
        {
            await EquipConfigs.EnsureStoneLevelLoaded();
            if (epoch != _lifecycleEpoch || !IsShown) return;

            if (!TrySendNextUpgrade())
            {
                _oneKeyUpgradeRunning = false;
                TipsManager.Toast("暂无可升级宝石");
            }
        }

        private bool TrySendNextUpgrade()
        {
            if (!_oneKeyUpgradeRunning || _upgradeRequestPending) return false;
            if (!TryFindNextUpgradeableStone(out GoodsStoneSlot slot)) return false;

            _upgradeRequestPending = true;
            GameLog.Info(
                "Equip",
                "一键升级串行发送 → UpgradeStone(equip_type={0},pos={1},upgrade_type=0)",
                _selectedEquipType,
                slot.Pos);
            EquipJewelController.Instance.UpgradeStone(_selectedEquipType, slot.Pos, 0, silentSuccess: true);
            return true;
        }

        private void OnStoneUpgradeCompleted(int equipPos, int stonePos, bool success)
        {
            if (!_oneKeyUpgradeRunning || equipPos != _selectedEquipType) return;

            _upgradeRequestPending = false;
            if (!success)
            {
                _oneKeyUpgradeRunning = false;
                GameLog.Info("Equip", "一键升级序列因15215失败停止 equip_type={0} pos={1}", equipPos, stonePos);
                return;
            }

            if (TrySendNextUpgrade()) return;
            _oneKeyUpgradeRunning = false;
            GameLog.Info("Equip", "一键升级序列完成 equip_type={0}", equipPos);
        }

        private bool TryFindNextUpgradeableStone(out GoodsStoneSlot selected)
        {
            selected = default;
            GoodsDetailVo vo = GoodsDynamicModel.Instance.Peek(_selectedGoodsId);
            if (vo?.StoneList == null || vo.StoneList.Count == 0) return false;

            var stones = new List<GoodsStoneSlot>(vo.StoneList);
            stones.Sort(CompareStoneUpgradeOrder);
            foreach (GoodsStoneSlot stone in stones)
            {
                if (stone.TypeId <= 0 || !CanUpgradeInstalledStone(stone.TypeId)) continue;
                selected = stone;
                return true;
            }
            return false;
        }

        private static int CompareStoneUpgradeOrder(GoodsStoneSlot left, GoodsStoneSlot right)
        {
            int leftLevel = EquipConfigs.TryGetStoneLevel(left.TypeId, out EquipConfigs.StoneLevel leftConfig)
                ? leftConfig.Level
                : int.MaxValue;
            int rightLevel = EquipConfigs.TryGetStoneLevel(right.TypeId, out EquipConfigs.StoneLevel rightConfig)
                ? rightConfig.Level
                : int.MaxValue;
            int levelCompare = leftLevel.CompareTo(rightLevel);
            return levelCompare != 0 ? levelCompare : left.Pos.CompareTo(right.Pos);
        }

        private static bool CanUpgradeInstalledStone(int typeId)
        {
            if (!EquipConfigs.TryGetStoneLevel(typeId, out EquipConfigs.StoneLevel config) || config.NextTypeId == 0)
                return false;

            // 已镶嵌的当前宝石自身计作一枚；剩余材料允许按老端规则递归折算更低级同系宝石。
            return HasUpgradeMaterials(typeId, config.NeedNum - 1, 0);
        }

        private static bool HasUpgradeMaterials(int typeId, long needNum, int depth)
        {
            if (needNum <= 0) return true;
            if (depth >= 16) return false;

            long owned = BagModel.Instance.GetTypeGoodsNum(typeId);
            if (owned >= needNum) return true;
            if (!EquipConfigs.TryGetStoneLevel(typeId, out EquipConfigs.StoneLevel config) || config.PreviousTypeId == 0)
                return false;

            long previousNeed = (long)config.NeedNum * needNum - owned;
            return previousNeed > 0 && HasUpgradeMaterials(config.PreviousTypeId, previousNeed, depth + 1);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        private void BindClick(Component target, System.Action onClick)
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
