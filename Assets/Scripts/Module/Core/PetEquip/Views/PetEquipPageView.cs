using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.OutWard;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.PetEquip.Views
{
    public enum PetEquipPageMode
    {
        Bag = 0,
        Strengthen = 1,
        Polish = 2,
    }

    /// <summary>
    /// 侍魂装备三页共用的可操作内容页。页面布局完全来自 PetEquipCreator；运行时只克隆 disabled 模板，
    /// 读取 16014 + 22/32/23/33 真实缓存，并把操作收口到 PetEquipController 16015/16/17。
    /// </summary>
    public sealed class PetEquipPageView : BaseView
    {
        public PetEquipPageMode mode;
        public TextMeshProUGUI lblHeading;
        public TextMeshProUGUI lblCombat;
        public TextMeshProUGUI lblSummary;
        public TextMeshProUGUI lblEmpty;
        public TextMeshProUGUI lblAction;
        public TextMeshProUGUI lblSelectAll;
        public Image btnAction;
        public Image btnSelectAll;
        public RectTransform wornContent;
        public RectTransform goodsContent;
        public PetEquipSlotRowView slotTemplate;
        public PetEquipGoodsRowView goodsTemplate;

        private readonly List<PetEquipSlotRowView> _slotRows = new List<PetEquipSlotRowView>(4);
        private readonly List<PetEquipGoodsRowView> _goodsRows = new List<PetEquipGoodsRowView>();
        private readonly List<BagGoods> _candidates = new List<BagGoods>();
        private readonly HashSet<long> _selectedCosts = new HashSet<long>();

        private int _typeId = PetEquipController.TYPE_HORSE;
        private int _selectedPos;
        private long _selectedBagGoodsId;
        private bool _subscribed;

        public int CurrentType => _typeId;
        public PetEquipPageMode Mode => mode;
        public int ActiveSlotCount => _slotRows.Count;
        public int ActiveGoodsCount => _goodsRows.Count;
        public long SelectedTargetGoodsId => FindEquipped(_selectedPos)?.GoodsId ?? 0;
        public int SelectedCostCount => mode == PetEquipPageMode.Strengthen
            ? _selectedCosts.Count
            : (_selectedBagGoodsId > 0 ? 1 : 0);

        protected override void OnInit()
        {
            if (slotTemplate != null) slotTemplate.gameObject.SetActive(false);
            if (goodsTemplate != null) goodsTemplate.gameObject.SetActive(false);
            UIUtil.AddClick(btnAction, OnAction);
            UIUtil.AddClick(btnSelectAll, OnSelectAll);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            RefreshNow();
            _ = EnsureConfigsThenRefresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            ClearSpawnedRows();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnDisable()
        {
            // BaseWindowSkin 的关闭按钮会直接隐藏整棵窗框，不会逐个调用缓存内容页 Hide。
            // 在父节点失活时补退订；下次 Flow.SelectTab -> Show 会重新订阅。
            Unsubscribe();
        }

        public void SetType(int typeId)
        {
            if (typeId != PetEquipController.TYPE_HORSE && typeId != PetEquipController.TYPE_PARTNER) return;
            if (_typeId != typeId)
            {
                _typeId = typeId;
                _selectedPos = 0;
                _selectedBagGoodsId = 0;
                _selectedCosts.Clear();
            }
            if (IsInitialized) RefreshNow();
        }

        public void RefreshNow()
        {
            NormalizeSelection();
            BuildSlots();
            BuildGoodsRows();
            RefreshHeaderAndActions();
        }

        private async Task EnsureConfigsThenRefresh()
        {
            await PetEquipConfigs.EnsureLoaded();
            await GoodsModel.EnsureLoaded();
            if (!this || !gameObject.activeInHierarchy) return;
            RefreshNow();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On<int>(GlobalEvent.EVT_PET_EQUIP_UPDATE, OnPetEquipUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_PET_EQUIP_BAG_UPDATE, OnPetEquipBagUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_PET_EQUIP_UPDATE, OnPetEquipUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_PET_EQUIP_BAG_UPDATE, OnPetEquipBagUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleUpdate);
        }

        private void OnPetEquipUpdate(int typeId)
        {
            if (!this) { Unsubscribe(); return; }
            if (typeId == _typeId && IsShown) RefreshNow();
        }

        private void OnPetEquipBagUpdate(int pos)
        {
            if (!this) { Unsubscribe(); return; }
            if (!IsShown) return;
            if (pos == WornContainer(_typeId) || pos == BagContainer(_typeId)) RefreshNow();
        }

        private void OnRoleUpdate()
        {
            if (!this) { Unsubscribe(); return; }
            if (IsShown) RefreshNow();
        }

        private void NormalizeSelection()
        {
            if (mode != PetEquipPageMode.Bag && FindEquipped(_selectedPos) == null)
            {
                _selectedPos = FirstEquippedPos();
                _selectedCosts.Clear();
                _selectedBagGoodsId = 0;
            }

            CollectCandidates(_candidates);
            if (mode == PetEquipPageMode.Strengthen)
            {
                _selectedCosts.RemoveWhere(id => !_candidates.Exists(g => g.GoodsId == id));
            }
            else if (_selectedBagGoodsId > 0 && !_candidates.Exists(g => g.GoodsId == _selectedBagGoodsId))
            {
                _selectedBagGoodsId = 0;
            }
        }

        private void BuildSlots()
        {
            ClearRows(_slotRows);
            if (wornContent == null || slotTemplate == null) return;
            for (int pos = 1; pos <= 4; pos++)
            {
                int capturedPos = pos;
                PetEquipSlotRowView row = Instantiate(slotTemplate, wornContent);
                row.gameObject.SetActive(true);
                PetEquipModel.PetEquipItem equipped = FindEquipped(pos);
                if (row.lblPosition != null) row.lblPosition.text = "部位 " + pos;
                if (row.lblDetail != null) row.lblDetail.text = equipped == null
                    ? "未穿戴"
                    : GoodsName(equipped.GoodsTypeId) + "\n" + equipped.Stage + "阶" + equipped.Star
                        + "星  强化+" + equipped.PosLevel;
                if (row.selectedMark != null)
                    row.selectedMark.gameObject.SetActive(mode != PetEquipPageMode.Bag && _selectedPos == pos);
                UIUtil.AddClick(row.click, () => SelectSlot(capturedPos));
                _slotRows.Add(row);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(wornContent);
        }

        private void BuildGoodsRows()
        {
            ClearRows(_goodsRows);
            if (goodsContent == null || goodsTemplate == null) return;
            for (int i = 0; i < _candidates.Count; i++)
            {
                BagGoods goods = _candidates[i];
                PetEquipGoodsRowView row = Instantiate(goodsTemplate, goodsContent);
                row.gameObject.SetActive(true);
                if (row.lblName != null) row.lblName.text = GoodsName(goods.TypeId) + (goods.GoodsNum > 1 ? " ×" + goods.GoodsNum : "");
                if (row.lblDetail != null) row.lblDetail.text = BuildGoodsDetail(goods);
                if (row.selectedMark != null) row.selectedMark.gameObject.SetActive(IsSelected(goods.GoodsId));
                UIUtil.AddClick(row.click, () => SelectGoods(goods));
                _goodsRows.Add(row);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(goodsContent);
        }

        private void RefreshHeaderAndActions()
        {
            PetEquipModel.PetEquipInfo info = PetEquipModel.Instance.Get(_typeId);
            string typeName = _typeId == PetEquipController.TYPE_HORSE ? "坐骑" : "伙伴";
            if (lblHeading != null)
            {
                string page = mode == PetEquipPageMode.Bag ? "装备背包"
                    : mode == PetEquipPageMode.Strengthen ? "装备强化" : "装备打造";
                lblHeading.text = typeName + page;
            }
            if (lblCombat != null) lblCombat.text = "战力 " + (info?.CombatPower ?? 0);
            if (lblEmpty != null) lblEmpty.gameObject.SetActive(_candidates.Count == 0);
            if (btnSelectAll != null) btnSelectAll.gameObject.SetActive(mode == PetEquipPageMode.Strengthen);

            bool enabled;
            string summary;
            string action;
            if (mode == PetEquipPageMode.Bag)
            {
                enabled = ValidateWear(false, out summary, out _);
                action = "穿戴";
            }
            else if (mode == PetEquipPageMode.Strengthen)
            {
                enabled = ValidateStrengthen(false, out summary);
                action = "强化";
                if (lblSelectAll != null) lblSelectAll.text = _selectedCosts.Count == _candidates.Count && _candidates.Count > 0
                    ? "取消全选" : "全选材料";
            }
            else
            {
                enabled = ValidatePolish(false, out summary);
                action = "打造";
            }
            if (lblSummary != null) lblSummary.text = summary;
            if (lblAction != null) lblAction.text = action;
            Button button = btnAction != null ? btnAction.GetComponent<Button>() : null;
            if (button != null) button.interactable = enabled;
        }

        private void SelectSlot(int pos)
        {
            if (mode == PetEquipPageMode.Bag) return;
            if (FindEquipped(pos) == null)
            {
                TipsManager.Toast("该部位需先穿戴装备");
                return;
            }
            if (_selectedPos == pos) return;
            _selectedPos = pos;
            _selectedCosts.Clear();
            _selectedBagGoodsId = 0;
            RefreshNow();
        }

        private void SelectGoods(BagGoods goods)
        {
            if (goods == null) return;
            if (mode == PetEquipPageMode.Strengthen)
            {
                if (!_selectedCosts.Add(goods.GoodsId)) _selectedCosts.Remove(goods.GoodsId);
            }
            else
            {
                _selectedBagGoodsId = _selectedBagGoodsId == goods.GoodsId ? 0 : goods.GoodsId;
            }
            RefreshNow();
        }

        private void OnSelectAll()
        {
            if (mode != PetEquipPageMode.Strengthen) return;
            if (_candidates.Count > 0 && _selectedCosts.Count == _candidates.Count)
            {
                _selectedCosts.Clear();
            }
            else
            {
                _selectedCosts.Clear();
                for (int i = 0; i < _candidates.Count; i++) _selectedCosts.Add(_candidates[i].GoodsId);
            }
            RefreshNow();
        }

        private void OnAction()
        {
            if (mode == PetEquipPageMode.Bag)
            {
                if (!ValidateWear(true, out _, out int pos)) return;
                PetEquipController.Instance.RequestWear(_typeId, pos, _selectedBagGoodsId);
                return;
            }
            if (mode == PetEquipPageMode.Strengthen)
            {
                if (!ValidateStrengthen(true, out _)) return;
                PetEquipModel.PetEquipItem target = FindEquipped(_selectedPos);
                var costs = new List<long>(_selectedCosts);
                PetEquipController.Instance.RequestStrengthen(_typeId, target.GoodsId, costs);
                return;
            }
            if (!ValidatePolish(true, out _)) return;
            PetEquipController.Instance.RequestPolish(_typeId, FindEquipped(_selectedPos).GoodsId, _selectedBagGoodsId);
        }

        private bool ValidateWear(bool toast, out string reason, out int pos)
        {
            pos = 0;
            BagGoods goods = FindCandidate(_selectedBagGoodsId);
            if (goods == null)
            {
                reason = "请选择一件背包装备";
                return Fail(toast, reason);
            }
            JObject cfg = PetEquipConfigs.GetGoods(goods.TypeId);
            pos = ReadInt(cfg, "pos");
            if (cfg == null || ReadInt(cfg, "type_id") != _typeId || pos < 1 || pos > 4)
            {
                reason = "所选物品不是当前类型的可穿戴装备";
                return Fail(toast, reason);
            }
            int roleLimit = ReadInt(cfg, "player_lv_limit");
            if (RoleModel.Instance.Level < roleLimit)
            {
                reason = "角色达到 " + roleLimit + " 级后可穿戴";
                return Fail(toast, reason);
            }
            int stageLimit = ReadInt(cfg, "pet_stage_limit");
            int currentStage = OutWardModel.Instance.Get(_typeId)?.Stage ?? 0;
            if (currentStage < stageLimit)
            {
                reason = (_typeId == 1 ? "坐骑" : "伙伴") + "达到 " + stageLimit + " 阶后可穿戴";
                return Fail(toast, reason);
            }
            reason = "将穿戴到部位 " + pos + "，服务器确认后刷新";
            return true;
        }

        private bool ValidateStrengthen(bool toast, out string reason)
        {
            PetEquipModel.PetEquipItem target = FindEquipped(_selectedPos);
            if (target == null)
            {
                reason = "请先选择一件已穿戴装备";
                return Fail(toast, reason);
            }
            JObject stage = PetEquipConfigs.GetStage(_typeId, target.PosId, target.Stage);
            int levelLimit = ReadInt(stage, "6");
            if (levelLimit > 0 && target.PosLevel >= levelLimit)
            {
                reason = "当前阶数的强化等级已达上限";
                return Fail(toast, reason);
            }
            if (_selectedCosts.Count == 0)
            {
                reason = _candidates.Count == 0 ? "暂无可吞噬的低评分装备" : "请选择至少一件强化材料";
                return Fail(toast, reason);
            }
            long exp = 0;
            foreach (long id in _selectedCosts)
            {
                BagGoods goods = FindCandidate(id);
                if (goods != null) exp += MaterialExperience(goods);
            }
            reason = "已选 " + _selectedCosts.Count + " 件，预计提供 " + exp + " 经验";
            return true;
        }

        private bool ValidatePolish(bool toast, out string reason)
        {
            PetEquipModel.PetEquipItem target = FindEquipped(_selectedPos);
            if (target == null)
            {
                reason = "请先选择一件已穿戴装备";
                return Fail(toast, reason);
            }
            BagGoods cost = FindCandidate(_selectedBagGoodsId);
            if (cost == null)
            {
                reason = _candidates.Count == 0 ? "暂无可提升该部位的打造材料" : "请选择一件打造材料";
                return Fail(toast, reason);
            }
            reason = "将消耗 " + GoodsName(cost.TypeId) + "，服务器确认后刷新";
            return true;
        }

        private static bool Fail(bool toast, string reason)
        {
            if (toast) TipsManager.Toast(reason);
            return false;
        }

        private void CollectCandidates(List<BagGoods> output)
        {
            output.Clear();
            IReadOnlyList<BagGoods> bag = BagModel.Instance.GetContainer(BagContainer(_typeId));
            if (mode == PetEquipPageMode.Bag)
            {
                for (int i = 0; i < bag.Count; i++) if (bag[i] != null && bag[i].GoodsNum > 0) output.Add(bag[i]);
                return;
            }

            PetEquipModel.PetEquipItem target = FindEquipped(_selectedPos);
            if (target == null) return;
            if (mode == PetEquipPageMode.Strengthen)
            {
                for (int i = 0; i < bag.Count; i++)
                {
                    BagGoods candidate = bag[i];
                    GoodsModel.GoodsBasic basic = candidate != null ? GoodsModel.GetGoodsBasicByTypeId(candidate.TypeId) : null;
                    if (candidate == null || candidate.GoodsNum <= 0 || basic == null || basic.Subtype > 4) continue;
                    int pos = GoodsPos(candidate, basic);
                    BagGoods worn = FindWornGoods(pos);
                    if (worn == null || GoodsRating(candidate) > GoodsRating(worn)) continue;
                    output.Add(candidate);
                }
                return;
            }

            for (int i = 0; i < bag.Count; i++)
            {
                BagGoods candidate = bag[i];
                GoodsModel.GoodsBasic basic = candidate != null ? GoodsModel.GetGoodsBasicByTypeId(candidate.TypeId) : null;
                if (candidate == null || candidate.GoodsNum <= 0 || basic == null) continue;
                if (basic.Subtype <= 4)
                {
                    if (GoodsPos(candidate, basic) != target.PosId) continue;
                    ResolveStageStar(candidate, out int stage, out int star);
                    if (stage > target.Stage || star > target.Star) output.Add(candidate);
                }
                else if (basic.Subtype == 5)
                {
                    JObject row = PetEquipConfigs.GetStage(_typeId, target.PosId, target.Stage);
                    if (MatchesCost(row, "7", candidate)) output.Add(candidate);
                }
                else if (basic.Subtype == 6)
                {
                    JObject row = PetEquipConfigs.GetStar(_typeId, target.PosId, target.Star);
                    if (MatchesCost(row, "5", candidate)) output.Add(candidate);
                }
            }
        }

        private static bool MatchesCost(JObject row, string key, BagGoods candidate)
        {
            string raw = row?[key]?.Value<string>();
            if (string.IsNullOrEmpty(raw)) return false;
            JArray costs;
            try { costs = JArray.Parse(raw); }
            catch { return false; }
            for (int i = 0; i < costs.Count; i++)
            {
                if (!(costs[i] is JObject cost)) continue;
                int typeId = ReadInt(cost, "1");
                long count = ReadLong(cost, "2");
                if (typeId == candidate.TypeId && candidate.GoodsNum >= count) return true;
            }
            return false;
        }

        private long MaterialExperience(BagGoods goods)
        {
            ResolveStageStar(goods, out int stage, out int star);
            int pos = GoodsPos(goods, GoodsModel.GetGoodsBasicByTypeId(goods.TypeId));
            return ReadLong(PetEquipConfigs.GetStage(_typeId, pos, stage), "5")
                + ReadLong(PetEquipConfigs.GetStar(_typeId, pos, star), "4");
        }

        private string BuildGoodsDetail(BagGoods goods)
        {
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return "物品 #" + goods.TypeId;
            if (basic.Subtype > 4) return basic.Subtype == 5 ? "升阶材料" : basic.Subtype == 6 ? "升星材料" : "打造材料";
            ResolveStageStar(goods, out int stage, out int star);
            return "部位 " + GoodsPos(goods, basic) + "  ·  " + stage + "阶" + star + "星  ·  评分 " + GoodsRating(goods);
        }

        private bool IsSelected(long goodsId)
        {
            return mode == PetEquipPageMode.Strengthen ? _selectedCosts.Contains(goodsId) : _selectedBagGoodsId == goodsId;
        }

        private BagGoods FindCandidate(long goodsId)
        {
            if (goodsId <= 0) return null;
            for (int i = 0; i < _candidates.Count; i++) if (_candidates[i].GoodsId == goodsId) return _candidates[i];
            return null;
        }

        private PetEquipModel.PetEquipItem FindEquipped(int pos)
        {
            if (pos < 1 || pos > 4) return null;
            PetEquipModel.PetEquipInfo info = PetEquipModel.Instance.Get(_typeId);
            if (info?.Items == null) return null;
            for (int i = 0; i < info.Items.Count; i++)
            {
                PetEquipModel.PetEquipItem item = info.Items[i];
                if (item.PosId == pos && item.GoodsId > 0) return item;
            }
            return null;
        }

        private int FirstEquippedPos()
        {
            for (int pos = 1; pos <= 4; pos++) if (FindEquipped(pos) != null) return pos;
            return 0;
        }

        private BagGoods FindWornGoods(int pos)
        {
            PetEquipModel.PetEquipItem item = FindEquipped(pos);
            if (item == null) return null;
            return BagModel.Instance.FindContainerGoods(WornContainer(_typeId), item.GoodsId);
        }

        private static long GoodsRating(BagGoods goods)
        {
            return goods == null ? 0 : goods.OverallRating != 0 ? goods.OverallRating : goods.Rating;
        }

        private int GoodsPos(BagGoods goods, GoodsModel.GoodsBasic basic)
        {
            JObject cfg = goods != null ? PetEquipConfigs.GetGoods(goods.TypeId) : null;
            int pos = ReadInt(cfg, "pos");
            return pos > 0 ? pos : basic?.EquipType ?? 0;
        }

        private static void ResolveStageStar(BagGoods goods, out int stage, out int star)
        {
            stage = goods?.EquipStage ?? 0;
            star = goods?.EquipStar ?? 0;
            if (goods == null || (stage > 0 && star > 0)) return;
            JObject cfg = PetEquipConfigs.GetGoods(goods.TypeId);
            if (stage <= 0) stage = ReadInt(cfg, "stage");
            if (star <= 0) star = ReadInt(cfg, "star");
        }

        private static int WornContainer(int typeId)
            => typeId == PetEquipController.TYPE_HORSE ? BagModel.POS_HORSE : BagModel.POS_PARTNER;

        private static int BagContainer(int typeId)
            => typeId == PetEquipController.TYPE_HORSE ? BagModel.POS_HORSE_BAG : BagModel.POS_PARTNER_BAG;

        private static string GoodsName(int typeId)
        {
            string name = GoodsModel.GetGoodsName(typeId);
            return string.IsNullOrEmpty(name) ? "物品 #" + typeId : name;
        }

        private static int ReadInt(JObject obj, string key)
        {
            JToken value = obj?[key];
            if (value == null || value.Type == JTokenType.Null) return 0;
            return int.TryParse(value.ToString(), out int result) ? result : 0;
        }

        private static long ReadLong(JObject obj, string key)
        {
            JToken value = obj?[key];
            if (value == null || value.Type == JTokenType.Null) return 0;
            return long.TryParse(value.ToString(), out long result) ? result : 0;
        }

        private void ClearSpawnedRows()
        {
            ClearRows(_slotRows);
            ClearRows(_goodsRows);
        }

        private static void ClearRows<T>(List<T> rows) where T : Component
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == null) continue;
                if (Application.isPlaying) Destroy(rows[i].gameObject);
                else DestroyImmediate(rows[i].gameObject);
            }
            rows.Clear();
        }
    }
}
