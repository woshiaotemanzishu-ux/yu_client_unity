using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备洗魄面板(对标老客户端 equipWash/EquipWashView.ts):左侧穿戴装备列表(Content/_Scroller1 铺 EquipWashItem)+
    /// 当前洗魄装备格(cur_wash_group)+ 洗魄属性条(wash_prop_group/EquipWashPropItem)+ 升段条件(_gp_up_cond:评分 _lb_cond_socre/阶数 _lb_cond_order)+
    /// 洗魄石/勾玉消耗(wash_stone_group/gp_purple)+ 额外保底道具(_gp_extra)+ 战力(_bit_figth)+ 洗魄/升段按钮(btn_wash/lb_wash)+ 强者礼包入口(giftIcon)。
    ///
    /// 洗魄协议(15212/15213/15214/15252,经 EquipWashController,自动循环 轮4 队列#4)已接线:btn_wash 按老端
    /// WashBtnCallBack 同一按钮兼二态——槽位已满(GoodsDetailVo.WashAttrs.Count 达 4,服务端 pt_152 guard 槽位范围
    /// 1-4)→ 升段(15252,is_buy 固定 0);未满 → 洗魄(15213,锁定位读 EquipWashModel,ratio_plus 固定 0/普通模式)。
    /// 当前装备列表尚未铺格，因此保持无选中态并阻断写协议，禁止退化为固定武器槽;
    /// 橙色以上属性二次确认(ConfirmDialog)/紫红橙保底模式选择/材料不够二次确认均依赖 config_equip_wash* 表
    /// (EquipConfigs 缺表,见其类注释)与背包材料计数联动,本轮不臆造,直接发送交服务端兜底(见 OnShow/BindWash 内 log)。
    ///
    /// 降级:EquipModel/GoodsModel/RoleManager 等数据、config_equip_wash 完整表均未移植 →
    /// 红点(_img_red)/各模板(_tpl_*)先隐藏;列表空、属性默认降级。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipWashView : EquipWashViewBind
    {
        /// <summary>服务端 15212 guard:洗魄槽位范围 1-4(pt_152 侦察报告);槽位数达此值视为"已满"走升段分支。</summary>
        private const int MaxWashSlots = 4;

        /// <summary>仅由真实装备列表点击建立，未选择时保持 0。</summary>
        private int _selectedEquipType;
        private long _selectedGoodsId;
        private readonly List<EquipWashItem> _equipmentItems = new List<EquipWashItem>(10);
        private readonly List<EquipWashPropItem> _propertyItems = new List<EquipWashPropItem>(4);
        private GameObject _equipmentTemplate;
        private EquipmentItem _currentEquipmentItem;
        [SerializeField] private Image _washGoodsHitArea;
        private bool _subscribed;
        private int _lifecycleEpoch;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            _selectedEquipType = 0;
            _selectedGoodsId = 0;
            ++_lifecycleEpoch;
            Subscribe();
            BuildRuntimeItems();
            RefreshEquipmentItems();
            if (!BagModel.Instance.HasEquipmentData) EquipWearController.Instance.RequestWornList();
            _ = EnsureConfigAndRefreshAsync(_lifecycleEpoch);
        }

        /// <summary>由真实装备列表点击建立选择；列表未落地时保持空，禁止固定操作武器槽。</summary>
        public void SelectEquipment(int equipType, long goodsId)
        {
            _selectedEquipType = equipType > 0 && goodsId > 0 ? equipType : 0;
            _selectedGoodsId = _selectedEquipType > 0 ? goodsId : 0;
            RefreshSelection();
            RefreshSelectedEquipment();
        }

        protected override void OnHide()
        {
            ++_lifecycleEpoch;
            EquipFlow.CloseSub("EquipWashGoodsView");
            Unsubscribe();
            foreach (EquipWashItem item in _equipmentItems)
                if (item != null && item.IsShown) item.Hide();
            foreach (EquipWashPropItem item in _propertyItems)
                if (item != null && item.IsShown) item.Hide();
            if (_currentEquipmentItem != null && _currentEquipmentItem.IsShown) _currentEquipmentItem.Hide();
            if (_Scroller1 != null)
            {
                _Scroller1.StopMovement();
                _Scroller1.horizontalNormalizedPosition = 0f;
            }
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            _equipmentItems.Clear();
            _propertyItems.Clear();
            _equipmentTemplate = null;
            _currentEquipmentItem = null;
            base.OnDispose();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentItems);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshEquipmentItems);
            EventDispatcher.On<long>(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, OnGoodsDetailUpdated);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_WASH_UPDATE, OnWashUpdated);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentItems);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshEquipmentItems);
            EventDispatcher.Off<long>(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, OnGoodsDetailUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_WASH_UPDATE, OnWashUpdated);
            _subscribed = false;
        }

        private async Task EnsureConfigAndRefreshAsync(int epoch)
        {
            await EquipConfigs.EnsureLoaded();
            if (this == null || !IsShown || epoch != _lifecycleEpoch) return;
            RefreshEquipmentItems();
        }

        private void BuildRuntimeItems()
        {
            EquipWashItem itemTemplate = EquipFlow.GetWashItemTemplate();
            EquipFlow.TryGetStrengthTemplates(out _, out _equipmentTemplate, out _);
            if (_equipmentItems.Count == 0 && itemTemplate != null && Content != null)
            {
                for (int equipType = 1; equipType <= 10; equipType++)
                {
                    GameObject go = Instantiate(itemTemplate.gameObject, Content, false);
                    go.name = "EquipWashItem_Runtime_" + equipType;
                    go.SetActive(false);
                    EquipWashItem item = go.GetComponent<EquipWashItem>();
                    if (item == null) { Destroy(go); continue; }
                    item.Show();
                    item.SetEquipPos(equipType);
                    item.SetSelectCallback(SelectEquipment);
                    _equipmentItems.Add(item);
                }
            }
            else
            {
                foreach (EquipWashItem item in _equipmentItems)
                    if (item != null && !item.IsShown) item.Show();
            }

            if (_propertyItems.Count == 0 && _tpl_EquipWashPropItem != null && wash_prop_group != null)
            {
                for (int index = 1; index <= MaxWashSlots; index++)
                {
                    GameObject go = Instantiate(_tpl_EquipWashPropItem, wash_prop_group, false);
                    go.name = "EquipWashPropItem_Runtime_" + index;
                    go.SetActive(false);
                    EquipWashPropItem item = go.GetComponent<EquipWashPropItem>();
                    if (item == null) { Destroy(go); continue; }
                    item.Show();
                    item.SetData(index, false, string.Empty);
                    _propertyItems.Add(item);
                }
            }
            else
            {
                foreach (EquipWashPropItem item in _propertyItems)
                    if (item != null && !item.IsShown) item.Show();
            }

            if (_currentEquipmentItem == null && _tpl_EquipmentItem != null && cur_wash_group != null)
            {
                GameObject go = Instantiate(_tpl_EquipmentItem, cur_wash_group, false);
                go.name = "EquipmentItem_Current_Runtime";
                go.SetActive(false);
                _currentEquipmentItem = go.GetComponent<EquipmentItem>();
            }
        }

        private void RefreshEquipmentItems()
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

            foreach (EquipWashItem item in _equipmentItems)
            {
                int equipType = item.EquipType;
                BagGoods worn = EquipAutoWear.GetWorn(equipType);
                bool configured = EquipConfigs.TryGetWashUnlockLv(equipType, out int unlockLv);
                item.SetUnlocked(!configured || RoleModel.Instance.Level >= unlockLv, unlockLv);
                GoodsDetailVo detail = worn != null ? GoodsDynamicModel.Instance.Peek(worn.GoodsId) : null;
                item.SetData(equipType, worn?.GoodsId ?? 0, worn?.TypeId ?? 0, detail?.Division ?? 0,
                    _equipmentTemplate, detail);
            }

            if (!selectedStillWorn)
            {
                BagGoods first = firstWorn > 0 ? EquipAutoWear.GetWorn(firstWorn) : null;
                _selectedEquipType = first != null ? firstWorn : 0;
                _selectedGoodsId = first?.GoodsId ?? 0;
            }
            RefreshSelection();
            RefreshSelectedEquipment();
        }

        private void RefreshSelection()
        {
            foreach (EquipWashItem item in _equipmentItems)
                item.SetSelect(item.EquipType == _selectedEquipType);
        }

        private void RefreshSelectedEquipment()
        {
            BagGoods worn = _selectedEquipType > 0 ? EquipAutoWear.GetWorn(_selectedEquipType) : null;
            if (_currentEquipmentItem != null)
            {
                if (worn != null)
                {
                    if (!_currentEquipmentItem.IsShown) _currentEquipmentItem.Show();
                    _currentEquipmentItem.SetData(worn.TypeId, 1);
                    _currentEquipmentItem.SetDisplayColor(worn.Color);
                    _currentEquipmentItem.SetClickCallBack(() => ItemTipsView.ShowEquipped(worn));
                }
                else if (_currentEquipmentItem.IsShown) _currentEquipmentItem.Hide();
            }

            if (worn == null)
            {
                ApplyDetail(null);
                return;
            }
            GoodsDetailVo cached = GoodsDynamicModel.Instance.Peek(worn.GoodsId);
            if (cached != null) ApplyDetail(cached);
            else
            {
                int epoch = _lifecycleEpoch;
                long goodsId = worn.GoodsId;
                GoodsDynamicModel.Instance.RequestDetail(goodsId, detail =>
                {
                    if (this == null || !IsShown || epoch != _lifecycleEpoch || goodsId != _selectedGoodsId) return;
                    ApplyDetail(detail);
                });
            }
        }

        private void ApplyDetail(GoodsDetailVo detail)
        {
            var byIndex = new Dictionary<int, GoodsWashAttr>();
            if (detail?.WashAttrs != null)
                foreach (GoodsWashAttr attr in detail.WashAttrs) byIndex[attr.Index] = attr;

            for (int i = 0; i < _propertyItems.Count; i++)
            {
                int index = i + 1;
                EquipWashPropItem item = _propertyItems[i];
                item.SetEquipType(_selectedEquipType);
                bool opened = byIndex.TryGetValue(index, out GoodsWashAttr washAttr)
                    || EquipWashModel.Instance.IsSlotOpened(_selectedEquipType, index);
                string text = opened && washAttr.AttrId > 0
                    ? GoodsModel.GetAttrName(washAttr.AttrId) + " +" + GoodsModel.FormatAttrValue(washAttr.AttrId, washAttr.AttrVal)
                    : string.Empty;
                item.SetData(index, opened, text);
            }

            int count = detail?.WashAttrs?.Count ?? 0;
            if (lb_wash != null) lb_wash.text = count >= MaxWashSlots ? "升段" : "洗魄";
            if (_bit_figth != null) _bit_figth.text = detail != null ? detail.WashRating.ToString() : "0";
            if (_lb_cond_socre != null) _lb_cond_socre.text = detail != null ? detail.WashRating.ToString() : "0";
            if (_lb_cond_order != null) _lb_cond_order.text = detail != null ? (detail.Division + "段") : "0段";
        }

        private void OnGoodsDetailUpdated(long goodsId)
        {
            if (goodsId != _selectedGoodsId) return;
            ApplyDetail(GoodsDynamicModel.Instance.Peek(goodsId));
            RefreshEquipmentItems();
        }

        private void OnWashUpdated()
        {
            if (_selectedGoodsId > 0)
                GoodsDynamicModel.Instance.RequestDetail(_selectedGoodsId, ApplyDetail);
            else ApplyDetail(null);
        }

        private void HideReds()
        {
            HideNode(_img_red);
        }

        private void HideTemplates()
        {
            if (_tpl_EquipWashPropItem != null) _tpl_EquipWashPropItem.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_GiftPushIcon != null) _tpl_GiftPushIcon.SetActive(false);
        }

        private void BindButtons()
        {
            BindWash(btn_wash, "洗魄/升段");
            BindMaterialSelector();
        }

        /// <summary>额外洗魄材料入口。老端绑定整个 _gp_extra；Unity 使用 Prefab 中置顶的 87x87 透明命中面。</summary>
        private void BindMaterialSelector()
        {
            if (_washGoodsHitArea == null)
            {
                GameLog.Error("Equip", "EquipWashView 缺少 Prefab 绑定 WashGoodsHitArea，材料入口保持阻断");
                return;
            }

            if (_gp_extra != null)
                foreach (Graphic graphic in _gp_extra.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = graphic == _washGoodsHitArea;
            UIUtil.ClearClicks(_washGoodsHitArea);
            UIUtil.AddClick(_washGoodsHitArea, OpenWashGoodsSelector);
        }

        private void OpenWashGoodsSelector()
        {
            if (_selectedEquipType <= 0 || _selectedGoodsId <= 0)
            {
                TipsManager.Toast("请先选择已穿戴装备");
                return;
            }
            int lockCount = EquipWashModel.Instance.GetLockedIndices(_selectedEquipType).Count;
            EquipFlow.OpenSub(nameof(EquipWashGoodsView),
                new EquipWashGoodsView.Context(_selectedEquipType, _selectedGoodsId, lockCount));
        }

        /// <summary>btn_wash → 已穿戴武器槽满 4 槽走升段(15252),否则洗魄(15213;锁定位读 EquipWashModel,
        /// 保底模式固定 0/普通,同 EquipStrenView 既有"无选中态先直发"简化处理)。未穿武器槽 → 跳过并日志。</summary>
        private void BindWash(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                if (_selectedEquipType <= 0 || _selectedGoodsId <= 0)
                {
                    TipsManager.Toast("请先选择已穿戴装备");
                    GameLog.Warn("Equip", "点击[{0}]被阻止：装备列表尚未建立真实选中态", label);
                    return;
                }
                if (!EquipConfigs.IsLoaded || !EquipConfigs.TryGetWashUnlockLv(_selectedEquipType, out _))
                {
                    TipsManager.Toast("洗魄配置未就绪");
                    GameLog.Warn("Equip", "点击[{0}]被阻止：config_equip_wash_unlock_lv 缺失", label);
                    return;
                }
                GoodsDetailVo vo = GoodsDynamicModel.Instance.Peek(_selectedGoodsId);
                bool slotsFull = vo != null && vo.WashAttrs != null && vo.WashAttrs.Count >= MaxWashSlots;
                if (slotsFull)
                {
                    GameLog.Info("Equip", "点击[{0}] → 槽位已满,UpgradeDivision(equip_type={1},isBuy=0)", label, _selectedEquipType);
                    EquipWashController.Instance.UpgradeDivision(_selectedEquipType, 0);
                }
                else
                {
                    List<int> locked = EquipWashModel.Instance.GetLockedIndices(_selectedEquipType);
                    GameLog.Info("Equip", "点击[{0}] → WashExecute(equip_type={1},lockCount={2},ratioPlus=0)",
                        label, _selectedEquipType, locked.Count);
                    EquipWashController.Instance.WashExecute(_selectedEquipType, locked, 0);
                }
            });
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
