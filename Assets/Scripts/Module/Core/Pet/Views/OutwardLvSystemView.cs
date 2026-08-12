using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Pet;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dress;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.OutWard;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Pet
{
    /// <summary>
    /// 系统B等级生产子页。Prefab 保存完整 OutwardLvSystem 结构；本类只消费 16028 权威状态、
    /// 投影经验/材料/满级/红点，动态建立三条技能，并由具名技能箭头执行 16030 资格闸。
    /// </summary>
    public sealed class OutwardLvSystemView : OutwardLvSystemBind
    {
        private readonly List<PetRoundItemBind> _skillItems = new List<PetRoundItemBind>();
        private readonly List<BaseAwardItem> _goodsItems = new List<BaseAwardItem>();
        private OutWardBaseView _owner;
        private int _typeId;
        private bool _subscribed;
        private bool _autoLeveling;
        private bool _levelRedEnabled = true;

        public void Open(OutWardBaseView owner, int typeId)
        {
            _owner = owner;
            _typeId = typeId;
            _levelRedEnabled = PlayerPrefs.GetInt(RedPreferenceKey(typeId), 1) != 0;
            Show(typeId);
        }

        public void RefreshView()
        {
            if (IsShown) RefreshAll();
        }

        protected override void OnInit()
        {
            BindClick(img_btn_group3, ToggleAutoLevel);
            BindClick(_gp_check, ToggleLevelRed);
            SetActive(_tpl_BaseAwardItem, false);
            SetActive(_tpl_PetRoundItem, false);
            SetActive(_tpl_PetEquipOutItem, false);
        }

        protected override void OnShow(object args)
        {
            if (args is int typeId && typeId > 0) _typeId = typeId;
            Subscribe();
            OutWardController.Instance.RequestLvPanel(_typeId);
            RefreshAll();
        }

        protected override void OnHide()
        {
            _autoLeveling = false;
            Unsubscribe();
            ClearDynamic();
        }

        protected override void OnDispose()
        {
            _autoLeveling = false;
            Unsubscribe();
            ClearDynamic();
        }

        public void Close()
        {
            OutWardBaseView owner = _owner;
            _owner = null;
            Hide();
            owner?.RestoreCapturedLevelSystem();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_OUTWARD_UPDATE, OnOutWardUpdate);
            EventDispatcher.On<OutWardTransactionResult>(GlobalEvent.EVT_OUTWARD_TRANSACTION_RESULT, OnTransactionResult);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_OUTWARD_UPDATE, OnOutWardUpdate);
            EventDispatcher.Off<OutWardTransactionResult>(GlobalEvent.EVT_OUTWARD_TRANSACTION_RESULT, OnTransactionResult);
        }

        private void OnOutWardUpdate()
        {
            if (this && IsShown) RefreshAll();
        }

        private void OnTransactionResult(OutWardTransactionResult result)
        {
            if (!this || !IsShown || result.TypeId != _typeId) return;
            RefreshAll();
            if (result.Command != Proto.OUTWARD_LV_UP || !_autoLeveling) return;
            if (!result.Success || !CanAutoContinue())
            {
                _autoLeveling = false;
                RefreshButton();
                return;
            }
            OutWardController.Instance.LvUp(_typeId);
        }

        private void RefreshAll()
        {
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            long needExp = vo == null ? 0 : OutWardConfigs.GetLevelNeedExp(_typeId, vo.Level);
            bool maxLevel = vo != null && vo.HasLv && !OutWardConfigs.HasLevel(_typeId, vo.Level + 1);
            if (lb_exp_progress != null)
                lb_exp_progress.text = vo == null || !vo.HasLv ? "加载中"
                    : maxLevel ? "已满级" : vo.CurExp + "/" + needExp;
            SetActive(maxGp, maxLevel);
            SetActive(runeGp, !maxLevel);
            SetActive(waterGp, !maxLevel);
            if (Round2 != null)
            {
                Round2.type = Image.Type.Filled;
                Round2.fillMethod = Image.FillMethod.Radial360;
                Round2.fillOrigin = (int)Image.Origin360.Top;
                Round2.fillClockwise = true;
                Round2.fillAmount = maxLevel ? 1f : needExp > 0 ? Mathf.Clamp01((float)vo.CurExp / needExp) : 0f;
            }
            RebuildGoods();
            RebuildSkills();
            RefreshButton();
            if (_gp_check != null) _gp_check.gameObject.SetActive(ServerTimeModel.GetOpenServerDay() >= 8);
            if (_img_check != null) _img_check.gameObject.SetActive(_levelRedEnabled);
        }

        private void RebuildGoods()
        {
            DestroyItems(_goodsItems);
            if (_tpl_BaseAwardItem == null || goods_group == null) return;
            IReadOnlyList<OutWardConfigs.TrainGoodsConfig> configs = OutWardConfigs.GetTrainGoods(_typeId);
            for (int i = 0; i < configs.Count; i++)
            {
                OutWardConfigs.TrainGoodsConfig config = configs[i];
                if (config.Type != 4) continue;
                GameObject go = Instantiate(_tpl_BaseAwardItem, goods_group, false);
                BaseAwardItem item = go.GetComponent<BaseAwardItem>();
                if (item == null) { Destroy(go); continue; }
                item.Show();
                item.SetData(config.GoodsId, CountInBag(config.GoodsId));
                _goodsItems.Add(item);
            }
            OutWardConfigs.TrainGoodsConfig primary = null;
            for (int i = 0; i < configs.Count; i++)
                if (configs[i].Type == 4) { primary = configs[i]; break; }
            if (goods_number != null) goods_number.text = primary == null ? string.Empty : CountInBag(primary.GoodsId).ToString();
            if (goods_icon != null && primary != null)
            {
                string icon = GoodsModel.GetGoodsIcon(primary.GoodsId);
                if (!string.IsNullOrEmpty(icon))
                    _ = ResManager.SetImageAsync(goods_icon, GameResPath.GetGoodsIconPath(icon), nativeSize: false);
            }
        }

        private void RebuildSkills()
        {
            DestroyItems(_skillItems);
            if (_tpl_PetRoundItem == null || lv_skill_group == null) return;
            IReadOnlyList<OutWardModel.LevelSkillRowState> rows = OutWardModel.Instance.GetLevelSkillRows(_typeId, 3);
            for (int i = 0; i < rows.Count; i++)
            {
                OutWardModel.LevelSkillRowState row = rows[i];
                GameObject go = Instantiate(_tpl_PetRoundItem, lv_skill_group, false);
                PetRoundItemBind item = go.GetComponent<PetRoundItemBind>();
                if (item == null) { Destroy(go); continue; }
                item.Show();
                if (item.bottom_text != null) item.bottom_text.text = row.Name;
                if (item.skill_lv != null) item.skill_lv.text = "Lv." + row.SkillLevel;
                if (item.red_dot != null) item.red_dot.gameObject.SetActive(row.CanUpgrade);
                if (item.up_arrow1 != null)
                {
                    item.up_arrow1.gameObject.SetActive(row.HasNextLevel);
                    item.up_arrow1.raycastTarget = row.CanUpgrade;
                    int upgradeSkillId = row.SkillId;
                    UIUtil.ClearClicks(item.up_arrow1);
                    UIUtil.AddClick(item.up_arrow1, () => OutWardController.Instance.TryLvSkillUp(
                        _typeId, upgradeSkillId, out _));
                }
                if (item.icon != null && !string.IsNullOrEmpty(row.Icon))
                    _ = ResManager.SetImageAsync(item.icon, GameResPath.GetSkillIcon(row.Icon), nativeSize: false);
                if (item.click_group != null)
                {
                    int detailSkillId = row.SkillId;
                    Graphic detailHit = item.click_group.GetComponent<Graphic>()
                        ?? item.click_group.GetComponentInChildren<Graphic>(true);
                    if (detailHit != null) UIUtil.ClearClicks(detailHit);
                    UIUtil.AddClick(item.click_group, () => DressSkillTipFlow.Show(detailSkillId));
                }
                _skillItems.Add(item);
            }
        }

        private void ToggleAutoLevel()
        {
            if (!CanLevelUp())
            {
                _autoLeveling = false;
                RefreshButton();
                return;
            }
            _autoLeveling = !_autoLeveling;
            RefreshButton();
            if (_autoLeveling) OutWardController.Instance.LvUp(_typeId);
        }

        /// <summary>老端按钮门槛：未满级且任一 type=4 经验物品持有量大于0即可发一次16029。</summary>
        private bool CanLevelUp()
        {
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            if (vo == null || !vo.HasLv || !OutWardConfigs.HasLevel(_typeId, vo.Level + 1)) return false;
            IReadOnlyList<OutWardConfigs.TrainGoodsConfig> configs = OutWardConfigs.GetTrainGoods(_typeId);
            for (int i = 0; i < configs.Count; i++)
                if (configs[i].Type == 4 && CountInBag(configs[i].GoodsId) > 0) return true;
            return false;
        }

        /// <summary>老端 LvsystemCanLvUp：仅材料总经验覆盖当前剩余经验时亮红点/连续下一次。</summary>
        private bool CanAutoContinue()
        {
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            if (vo == null || !vo.HasLv || !OutWardConfigs.HasLevel(_typeId, vo.Level + 1)) return false;
            long remaining = Math.Max(0, OutWardConfigs.GetLevelNeedExp(_typeId, vo.Level) - vo.CurExp);
            IReadOnlyList<OutWardConfigs.TrainGoodsConfig> configs = OutWardConfigs.GetTrainGoods(_typeId);
            long supplied = 0;
            for (int i = 0; i < configs.Count; i++)
                if (configs[i].Type == 4) supplied += CountInBag(configs[i].GoodsId) * Math.Max(0, configs[i].Exp);
            return remaining <= 0 || supplied >= remaining;
        }

        private void RefreshButton()
        {
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            bool maxLevel = vo != null && vo.HasLv && !OutWardConfigs.HasLevel(_typeId, vo.Level + 1);
            bool canLevel = CanLevelUp();
            bool showRed = CanAutoContinue();
            if (lb_btn_group3 != null) lb_btn_group3.text = maxLevel ? "已满级" : _autoLeveling ? "停止升级" : "一键升级";
            if (img_btn_group3_red != null) img_btn_group3_red.gameObject.SetActive(_levelRedEnabled && showRed);
            SetInteractable(img_btn_group3, !maxLevel);
        }

        private void ToggleLevelRed()
        {
            _levelRedEnabled = !_levelRedEnabled;
            PlayerPrefs.SetInt(RedPreferenceKey(_typeId), _levelRedEnabled ? 1 : 0);
            PlayerPrefs.Save();
            RefreshButton();
            if (_img_check != null) _img_check.gameObject.SetActive(_levelRedEnabled);
        }

        private static string RedPreferenceKey(int typeId) => "outward.lv.red." + typeId;

        private static long CountInBag(int goodsId)
        {
            long total = 0;
            foreach (Bag.BagGoods goods in Bag.BagModel.Instance.BagGoodsList)
                if (goods.TypeId == goodsId) total += goods.GoodsNum;
            return total;
        }

        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (graphic == null) return;
            graphic.raycastTarget = true;
            UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(graphic, action);
        }

        private static void SetInteractable(Component target, bool value)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (graphic != null) graphic.raycastTarget = value;
            Button button = target.GetComponent<Button>();
            if (button != null) button.interactable = value;
        }

        private static void SetActive(Component target, bool value) { if (target != null) target.gameObject.SetActive(value); }
        private static void SetActive(GameObject target, bool value) { if (target != null) target.SetActive(value); }

        private void ClearDynamic()
        {
            DestroyItems(_skillItems);
            DestroyItems(_goodsItems);
        }

        private static void DestroyItems<T>(List<T> items) where T : Component
        {
            for (int i = 0; i < items.Count; i++) if (items[i] != null) Destroy(items[i].gameObject);
            items.Clear();
        }
    }
}
