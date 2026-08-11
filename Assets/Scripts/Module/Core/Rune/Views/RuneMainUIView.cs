using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Rune;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Composite;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.RuneTreasure;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>九霄劫魄主页面；当前 Prefab 是唯一视觉树，本类只负责绑定、数据和协议动作。</summary>
    public sealed class RuneMainUIView : RuneMainUIViewBind
    {
        private readonly List<RuneIconBind> _slotViews = new List<RuneIconBind>();
        private readonly Dictionary<int, RuneSpIconBind> _specialSlotViews =
            new Dictionary<int, RuneSpIconBind>();
        private readonly Dictionary<Image, Sprite> _ownedSprites = new Dictionary<Image, Sprite>();
        private FightingShowSmallItem _fightItem;
        private int _selectedPosition = 1;
        private int _renderEpoch;
        private bool _subscribed;

        public int SelectedPosition => _selectedPosition;

        protected override void OnInit()
        {
            HideTemplates();
            BuildSlots();
            BuildFighting();
            BindButtons();
            HideNode(convertDot);
        }

        protected override void OnShow(object args)
        {
            SetRuntimeChildrenShown(true);
            Subscribe();
            RuneController.Instance.Init();
            RuneController.Instance.RequestInfo();
            RuneController.Instance.RequestRuneBag();
            Render();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            SetRuntimeChildrenShown(false);
        }

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

        private void BuildSlots()
        {
            RectTransform[] containers =
            {
                conta1, conta2, conta3, conta4, conta5,
                conta6, conta7, conta8, conta9, conta10,
            };
            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i] == null) continue;
                bool special = i >= 8 && _tpl_RuneSpIcon != null;
                GameObject template = special ? _tpl_RuneSpIcon : _tpl_RuneIcon;
                if (template == null) continue;
                GameObject clone = Instantiate(template, containers[i], false);
                clone.name = "RuneSlot" + (i + 1);
                clone.SetActive(true);
                int position = i + 1;
                if (special)
                {
                    RuneSpIconBind specialView = clone.GetComponent<RuneSpIconBind>()
                        ?? clone.GetComponentInChildren<RuneSpIconBind>(true);
                    if (specialView == null) { Destroy(clone); continue; }
                    specialView.Show();
                    BindClick(specialView._Image2, () => SelectPosition(position));
                    BindClick(specialView._img_icon, () => SelectPosition(position));
                    _specialSlotViews[position] = specialView;
                    continue;
                }
                RuneIconBind view = clone.GetComponent<RuneIconBind>()
                    ?? clone.GetComponentInChildren<RuneIconBind>(true);
                if (view == null) { Destroy(clone); continue; }
                view.Show();
                BindClick(view.bg, () => SelectPosition(position));
                BindClick(view._img_icon, () => SelectPosition(position));
                _slotViews.Add(view);
            }
        }

        private void BuildFighting()
        {
            if (_fightItem != null || _tpl_FightingShowSmallItem == null || _fight_con == null) return;
            GameObject clone = Instantiate(_tpl_FightingShowSmallItem, _fight_con, false);
            clone.name = "FightingShowSmallItem_Rune";
            clone.SetActive(true);
            _fightItem = clone.GetComponent<FightingShowSmallItem>()
                ?? clone.GetComponentInChildren<FightingShowSmallItem>(true);
            if (_fightItem == null) { Destroy(clone); return; }
            _fightItem.Show();
        }

        private void BindButtons()
        {
            BindClick(convertBtn, () => RuneFlow.ToggleSub(nameof(RuneConvertView)));
            BindClick(lookBtn, () => RuneFlow.ToggleSub(nameof(RunePropertyView)));
            BindClick(skillBtn, () => RuneFlow.ToggleSub(nameof(RuneSkillView)));
            BindClick(awakeBtn, () => RuneFlow.ToggleSub(nameof(RuneAwakenView)));
            BindClick(resolveBtn, () => RuneFlow.ToggleSub(nameof(RuneDecMainView)));
            BindClick(composeBtn, () => { RuneFlow.Close(); CompositeFlow.Open(); });
            BindClick(replaceBtn, () => OpenBag(true));
            BindClick(insertBtn, () => OpenBag(false));
            BindClick(upgradeBtn, UpgradeSelected);
            BindClick(goBtn, DungeonRuneShellView.Show);
            BindClick(jumpBox, DungeonRuneShellView.Show);
            BindClick(_btn_fb, DungeonRuneShellView.Show);
            BindClick(_btn_xb, RuneTreasureFlow.Open);
        }

        private void SelectPosition(int position)
        {
            _selectedPosition = Mathf.Clamp(position, 1, 10);
            RuneModel.SlotVo value = RuneModel.Instance.GetSlot(_selectedPosition);
            if (value != null && value.IfOpen && !value.IsWorn) OpenBag(false);
            else Render();
        }

        private void OpenBag(bool replace) => RuneFlow.OpenRuneBag(_selectedPosition, replace);

        private void UpgradeSelected()
        {
            RuneModel.SlotVo value = RuneModel.Instance.GetSlot(_selectedPosition);
            if (value == null || !value.IsWorn)
            {
                TipsManager.Toast("请先镶嵌灵魄");
                return;
            }
            int subtype = GoodsModel.GetGoodsBasicByTypeId(value.GoodsTypeId)?.Subtype ?? 0;
            int cost = RuneConfigs.GetUpgradeCost(subtype, value.Color, value.Lv + 1);
            if (cost <= 0)
            {
                TipsManager.Toast("灵魄已达到当前上限");
                return;
            }
            if (RuneModel.Instance.RunePoint < cost)
            {
                TipsManager.Toast("灵魄经验不足");
                return;
            }
            RuneController.Instance.Upgrade(value.GoodsId);
        }

        private void Render()
        {
            int epoch = ++_renderEpoch;
            RuneModel model = RuneModel.Instance;
            ResolveDefaultSelection(model);
            for (int i = 0; i < _slotViews.Count; i++) RenderSlot(_slotViews[i], i + 1, epoch);
            foreach (KeyValuePair<int, RuneSpIconBind> pair in _specialSlotViews)
                RenderSpecialSlot(pair.Value, pair.Key, epoch);
            RenderSelected(epoch);
            RenderReds();
            if (_fightItem != null) _fightItem.SetFighting(model.SumPower);
        }

        private void RenderSpecialSlot(RuneSpIconBind view, int position, int epoch)
        {
            RuneModel.SlotVo value = RuneModel.Instance.GetSlot(position);
            bool worn = value != null && value.IfOpen && value.IsWorn;
            SetActive(view._Image_add, value != null && value.IfOpen && !worn);
            if (view._lb_name != null) view._lb_name.text = worn ? GoodsName(value.GoodsTypeId) : string.Empty;
            if (!worn)
            {
                ClearSprite(view._img_icon);
                return;
            }
            int color = DisplayColor(value.GoodsTypeId, value.Color);
            _ = SetSpriteAsync(view._img_icon, RuneCardPath(value.GoodsTypeId), epoch);
            _ = SetSpriteAsync(view._img_kuang, RuneFramePath(color), epoch);
            _ = SetSpriteAsync(view._img_iconbg, RuneBackgroundPath(color), epoch);
        }

        private void ResolveDefaultSelection(RuneModel model)
        {
            RuneModel.SlotVo current = model.GetSlot(_selectedPosition);
            if (current != null && current.IfOpen) return;
            for (int i = 1; i <= 10; i++)
            {
                RuneModel.SlotVo value = model.GetSlot(i);
                if (value != null && value.IfOpen && (!value.IsWorn || CanUpgrade(value)))
                {
                    _selectedPosition = i;
                    return;
                }
            }
            for (int i = 1; i <= 10; i++)
            {
                RuneModel.SlotVo value = model.GetSlot(i);
                if (value != null && value.IfOpen) { _selectedPosition = i; return; }
            }
        }

        private void RenderSlot(RuneIconBind view, int position, int epoch)
        {
            RuneModel.SlotVo value = RuneModel.Instance.GetSlot(position);
            bool open = value != null && value.IfOpen;
            bool worn = open && value.IsWorn;
            SetActive(view.unlockBox, !open);
            SetActive(view.icon_wnd, open);
            SetActive(view._Image_add, open && !worn);
            SetActive(view.awakeImg, worn && HasAwake(value));
            SetActive(view.selectBg, position == _selectedPosition);
            SetActive(view.red_dot, open && (!worn || CanUpgrade(value)));
            if (view.unlockLab != null)
            {
                RuneConfigs.TryGetPosition(position, out RuneConfigs.PositionRow row);
                view.unlockLab.text = row != null && row.TowerFloor > 0
                    ? "通关" + row.TowerFloor + "层解锁"
                    : "未解锁";
            }
            if (view.lv != null) view.lv.text = worn ? value.Lv.ToString() : string.Empty;
            if (view._lb_name != null) view._lb_name.text = worn ? GoodsName(value.GoodsTypeId) : string.Empty;
            if (worn)
            {
                int color = DisplayColor(value.GoodsTypeId, value.Color);
                _ = SetSpriteAsync(view._img_icon, RuneCardPath(value.GoodsTypeId), epoch);
                _ = SetSpriteAsync(view._img_kuang, RuneFramePath(color), epoch);
                _ = SetSpriteAsync(view._img_iconbg, RuneBackgroundPath(color), epoch);
            }
            else
            {
                ClearSprite(view._img_icon);
            }
        }

        private void RenderSelected(int epoch)
        {
            RuneModel.SlotVo value = RuneModel.Instance.GetSlot(_selectedPosition);
            bool open = value != null && value.IfOpen;
            bool worn = open && value.IsWorn;
            SetActive(_gp_card, worn);
            SetActive(insert_conta, open && !worn);
            SetActive(active_conta, !open);
            SetActive(replaceBtn, worn);
            SetActive(upgradeBtn, worn);
            SetActive(goBtn, !open);
            SetActive(pro_conta, worn);
            SetActive(gp_sp_skill, worn && IsSpecial(value));
            if (!worn)
            {
                if (condition != null)
                {
                    RuneConfigs.TryGetPosition(_selectedPosition, out RuneConfigs.PositionRow row);
                    condition.text = !open && row != null && row.TowerFloor > 0
                        ? "通关九劫塔第" + row.TowerFloor + "层解锁"
                        : string.Empty;
                }
                if (_lb_name != null) _lb_name.text = string.Empty;
                if (_lb_card != null) _lb_card.text = string.Empty;
                if (left_pro != null) left_pro.text = string.Empty;
                if (right_pro != null) right_pro.text = string.Empty;
                ClearSprite(_img_card);
                return;
            }

            int color = DisplayColor(value.GoodsTypeId, value.Color);
            _ = SetSpriteAsync(_img_card, RuneCardPath(value.GoodsTypeId), epoch);
            _ = SetSpriteAsync(_img_kuang, RuneFramePath(color), epoch);
            _ = SetSpriteAsync(_img_cardbg, RuneBackgroundPath(color), epoch);
            if (_lb_name != null) _lb_name.text = GoodsName(value.GoodsTypeId);
            if (_lb_card != null) _lb_card.text = value.Lv + "级";
            if (top_level != null) top_level.text = value.Lv.ToString();

            int subtype = GoodsModel.GetGoodsBasicByTypeId(value.GoodsTypeId)?.Subtype ?? 0;
            IReadOnlyList<RuneConfigs.AttrValue> next = WithAwakenBonus(
                RuneConfigs.GetComputedAttributes(subtype, value.Color, value.Lv + 1), value);
            IReadOnlyList<RuneConfigs.AttrValue> current = WithAwakenBonus(
                RuneConfigs.GetComputedAttributes(subtype, value.Color, value.Lv), value);
            if (_lb_title1 != null) _lb_title1.text = "当前属性";
            if (_lb_title2 != null) _lb_title2.text = next.Count > 0 ? "下一级" : "已满级";
            if (left_pro != null) left_pro.text = BuildAttributeText(current);
            if (right_pro != null) right_pro.text = BuildAttributeText(next);
            SetActive(arrow1, current.Count > 0 && next.Count > 0);
            SetActive(arrow2, current.Count > 1 && next.Count > 1);
            SetActive(top_level, next.Count == 0);
            int cost = RuneConfigs.GetUpgradeCost(subtype, value.Color, value.Lv + 1);
            SetActive(cost_conta, cost > 0);
            if (_cost_name != null) _cost_name.text = "灵魄经验";
            if (num != null) num.text = cost > 0 ? RuneModel.Instance.RunePoint + "/" + cost : "已满级";
            if (condition != null) condition.text = next.Count > 0 ? "下一级属性提升" : "已达到当前上限";
            if (lockLb != null)
            {
                int skillId = RuneConfigs.GetSkillIdForSubtype(subtype);
                string description = RuneConfigs.GetSkillCondition(skillId, Math.Max(1, RuneModel.Instance.SkillLv));
                lockLb.text = string.IsNullOrEmpty(description)
                    ? (RuneModel.Instance.SkillLv > 0 ? "技能 " + RuneModel.Instance.SkillLv + "级" : "技能尚未激活")
                    : description;
            }
        }

        private void RenderReds()
        {
            RuneModel.SlotVo value = RuneModel.Instance.GetSlot(_selectedPosition);
            bool insert = value != null && value.IfOpen && !value.IsWorn && HasCompatibleBagRune(value.PosId);
            bool upgrade = value != null && value.IsWorn && CanUpgrade(value);
            SetActive(insertDot, insert);
            SetActive(replaceDot, value != null && value.IsWorn && HasCompatibleBagRune(value.PosId));
            SetActive(upgradeDot, upgrade);
            SetActive(awakeRed, false);
            SetActive(skillRed, false);
            SetActive(composeDot, false);
            SetActive(resolveDot, false);
            SetActive(_img_skill_lock, RuneModel.Instance.SkillLv <= 0);
        }

        private static bool HasCompatibleBagRune(int position)
        {
            foreach (RuneModel.BagGoodsVo item in RuneModel.Instance.RuneBagGoods)
                if (item.Num > 0 && IsCompatible(position, item.TypeId)) return true;
            return false;
        }

        internal static bool IsCompatible(int position, int typeId)
        {
            GoodsModel.GoodsBasic goods = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (goods == null || !RuneConfigs.TryGetPosition(position, out RuneConfigs.PositionRow row)) return false;
            if (row.ExcludedSubtypes.Contains(goods.Subtype)) return false;
            if (row.IncludedSubtypes.Count > 0 && !row.IncludedSubtypes.Contains(goods.Subtype)) return false;
            foreach (RuneModel.SlotVo worn in RuneModel.Instance.Slots)
            {
                if (!worn.IsWorn || worn.PosId == position) continue;
                GoodsModel.GoodsBasic other = GoodsModel.GetGoodsBasicByTypeId(worn.GoodsTypeId);
                if (other != null && other.Subtype == goods.Subtype) return false;
            }
            return true;
        }

        private static bool CanUpgrade(RuneModel.SlotVo value)
        {
            if (value == null || !value.IsWorn) return false;
            int subtype = GoodsModel.GetGoodsBasicByTypeId(value.GoodsTypeId)?.Subtype ?? 0;
            int cost = RuneConfigs.GetUpgradeCost(subtype, value.Color, value.Lv + 1);
            return cost > 0 && RuneModel.Instance.RunePoint >= cost;
        }

        private static bool HasAwake(RuneModel.SlotVo value)
        {
            if (value?.Attrs == null) return false;
            for (int i = 0; i < value.Attrs.Count; i++) if (value.Attrs[i].AwakeLv > 0) return true;
            return false;
        }

        private static bool IsSpecial(RuneModel.SlotVo value)
        {
            int subtype = GoodsModel.GetGoodsBasicByTypeId(value?.GoodsTypeId ?? 0)?.Subtype ?? 0;
            return RuneConfigs.GetSkillIdForSubtype(subtype) > 0;
        }

        private static int DisplayColor(int typeId, int fallback)
        {
            int color = GoodsModel.GetColor(typeId);
            if (color <= 0) color = fallback;
            return (typeId == 26260005 || typeId == 26270005) ? 6 : Mathf.Clamp(color, 1, 6);
        }

        private static string GoodsName(int typeId)
        {
            string value = GoodsModel.GetGoodsName(typeId);
            return string.IsNullOrEmpty(value) ? typeId.ToString() : value;
        }

        private static string RuneCardPath(int typeId) => "resource/game/runeCard/" + typeId + ".png";
        private static string RuneFramePath(int color) => "resource/game/runeCard/icon_kp_0" + color + ".png";
        private static string RuneBackgroundPath(int color) => "resource/game/runeCard/icon_kpbg_0" + color + ".png";

        private async Task SetSpriteAsync(Image image, string path, int epoch)
        {
            if (image == null || string.IsNullOrEmpty(path)) return;
            Sprite sprite = await ResManager.LoadAsync<Sprite>(path);
            if (sprite == null) return;
            if (epoch != _renderEpoch || image == null)
            {
                ResManager.Release(sprite);
                return;
            }
            if (_ownedSprites.TryGetValue(image, out Sprite old) && old != null && old != sprite) ResManager.Release(old);
            _ownedSprites[image] = sprite;
            image.sprite = sprite;
            image.enabled = true;
        }

        private void ClearSprite(Image image)
        {
            if (image == null) return;
            if (_ownedSprites.TryGetValue(image, out Sprite old) && old != null) ResManager.Release(old);
            _ownedSprites.Remove(image);
            image.sprite = null;
            image.enabled = false;
        }

        private static IReadOnlyList<RuneConfigs.AttrValue> WithAwakenBonus(
            IReadOnlyList<RuneConfigs.AttrValue> source, RuneModel.SlotVo slot)
        {
            if (source == null || source.Count == 0) return Array.Empty<RuneConfigs.AttrValue>();
            var result = new List<RuneConfigs.AttrValue>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                RuneConfigs.AttrValue value = source[i];
                int awakeLevel = 0;
                if (slot?.Attrs != null)
                    for (int j = 0; j < slot.Attrs.Count; j++)
                        if (slot.Attrs[j].AttrId == value.AttrId) awakeLevel = slot.Attrs[j].AwakeLv;
                result.Add(new RuneConfigs.AttrValue(value.AttrId,
                    value.Value + RuneConfigs.GetAwakenBonus(slot?.Color ?? 0, value.AttrId, awakeLevel)));
            }
            return result;
        }

        private static string BuildAttributeText(IReadOnlyList<RuneConfigs.AttrValue> attrs)
        {
            if (attrs == null || attrs.Count == 0) return string.Empty;
            var lines = new List<string>(attrs.Count);
            for (int i = 0; i < attrs.Count; i++)
            {
                RuneConfigs.AttrValue value = attrs[i];
                lines.Add(GoodsModel.GetAttrName(value.AttrId) + " " +
                          GoodsModel.FormatAttrValue(value.AttrId, value.Value));
            }
            return string.Join("\n", lines);
        }

        private static void BindClick(Component target, Action action)
        {
            if (target == null || action == null) return;
            Image image = target as Image ?? target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void HideNode(Component component) => SetActive(component, false);

        private void HideTemplates()
        {
            if (_tpl_RuneSpIcon != null) _tpl_RuneSpIcon.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_RuneIcon != null) _tpl_RuneIcon.SetActive(false);
        }

        private void SetRuntimeChildrenShown(bool shown)
        {
            for (int i = 0; i < _slotViews.Count; i++)
            {
                RuneIconBind view = _slotViews[i];
                if (view == null) continue;
                if (shown && !view.IsShown) view.Show();
                else if (!shown && view.IsShown) view.Hide();
            }
            foreach (RuneSpIconBind view in _specialSlotViews.Values)
            {
                if (view == null) continue;
                if (shown && !view.IsShown) view.Show();
                else if (!shown && view.IsShown) view.Hide();
            }
            if (_fightItem == null) return;
            if (shown && !_fightItem.IsShown) _fightItem.Show();
            else if (!shown && _fightItem.IsShown) _fightItem.Hide();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            SetRuntimeChildrenShown(false);
            _renderEpoch++;
            foreach (Sprite sprite in _ownedSprites.Values) if (sprite != null) ResManager.Release(sprite);
            _ownedSprites.Clear();
        }
    }
}
