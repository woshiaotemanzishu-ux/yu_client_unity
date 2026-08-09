using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Activity;
using Shenxiao.Module.Core.CustomActivity;
using UnityEngine;

namespace Shenxiao.Module.Core.Activity
{
    public sealed class ConRechargeView : ConRechargeViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private CustomActivityModel.ActEntry _info;
        private int _tier = 1;

        protected override void OnInit()
        {
            if (_tpl_ConRechargeItem != null) _tpl_ConRechargeItem.SetActive(false);
            if (_tpl_ConRechargeGradeItem != null && _tpl_ConRechargeGradeItem != grade_item?.gameObject)
                _tpl_ConRechargeGradeItem.SetActive(false);
            HideSharedTemplates();
            ActivityViewUtil.BindClick(_btn_1, () => SelectTier(1));
            ActivityViewUtil.BindClick(_btn_2, () => SelectTier(2));
            ActivityViewUtil.BindClick(_btn_3, () => SelectTier(3));
        }

        protected override void OnShow(object args)
        {
            _info = args as CustomActivityModel.ActEntry;
            _tier = 1;
            EventDispatcher.On<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail);
            EventDispatcher.On<int, int, int>(GlobalEvent.EVT_CUSTOMACT_RESULT, OnResult);
            if (_info != null)
            {
                CustomActivityController.Instance.RequestActDetail(_info.BaseType, _info.SubType);
                int day = (int)Math.Max(0, (TimeUtil.NowSec() - _info.Stime) / 86400);
                CustomActivityController.Instance.RequestRechargeHistory(day);
            }
            Refresh(false);
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail);
            EventDispatcher.Off<int, int, int>(GlobalEvent.EVT_CUSTOMACT_RESULT, OnResult);
            ActivityViewUtil.HideAndDestroy(_cells);
        }

        private void OnDetail(int baseType, int subType)
        {
            if (_info != null && baseType == _info.BaseType && subType == _info.SubType) Refresh(false);
        }

        private void OnResult(int baseType, int subType, int code)
        {
            if (_info != null && baseType == _info.BaseType && subType == _info.SubType) Refresh(true);
        }

        private void SelectTier(int tier)
        {
            if (_tier == tier) return;
            _tier = tier;
            Refresh(true);
        }

        private void Refresh(bool preserveTier)
        {
            ActivityViewUtil.HideAndDestroy(_cells);
            if (_info == null) return;
            List<CustomActivityModel.DetailReward> rewards = ActivityViewUtil.Ordered(CustomActivityModel.Instance.GetDetail(_info.BaseType, _info.SubType));
            bool[] tiers = new bool[4];
            int claimTier = 0;
            foreach (CustomActivityModel.DetailReward reward in rewards)
            {
                int tier = TierOf(reward);
                tiers[tier] = true;
                if (claimTier == 0 && reward.Status == 1) claimTier = tier;
            }
            if (!preserveTier && claimTier != 0) _tier = claimTier;
            if (!tiers[_tier])
                for (int i = 1; i <= 3; i++) if (tiers[i]) { _tier = i; break; }

            ConfigureTab(1, tiers[1], rewards);
            ConfigureTab(2, tiers[2], rewards);
            ConfigureTab(3, tiers[3], rewards);

            CustomActivityModel.DetailReward gradeReward = null;
            foreach (CustomActivityModel.DetailReward reward in rewards)
            {
                if (TierOf(reward) != _tier) continue;
                if (ActivityViewUtil.ConditionInt(reward.Condition, "grade") == 1 && gradeReward == null)
                {
                    gradeReward = reward;
                    continue;
                }
                CreateListItem(reward);
            }
            ConfigureGrade(gradeReward, rewards);
            ActivityViewUtil.ResetTop(_group_item);
        }

        private void CreateListItem(CustomActivityModel.DetailReward reward)
        {
            if (_group_item == null || _group_item.content == null || _tpl_ConRechargeItem == null) return;
            GameObject go = Instantiate(_tpl_ConRechargeItem, _group_item.content);
            go.name = "ConRechargeItem_" + reward.Grade;
            ConRechargeItem item = go.GetComponent<ConRechargeItem>();
            if (item != null) { item.Show(); item.SetData(_info, reward); }
            else go.SetActive(true);
            _cells.Add(go);
        }

        private void ConfigureGrade(CustomActivityModel.DetailReward reward, List<CustomActivityModel.DetailReward> all)
        {
            if (grade_item == null) return;
            ConRechargeGradeItem item = grade_item.GetComponent<ConRechargeGradeItem>();
            if (reward == null || item == null)
            {
                grade_item.gameObject.SetActive(false);
                return;
            }
            int complete = 0;
            foreach (CustomActivityModel.DetailReward value in all)
                if (TierOf(value) == _tier && (value.Status == 1 || value.Status == 2)) complete++;
            item.Show();
            item.SetData(_info, reward, complete);
        }

        private void ConfigureTab(int tier, bool active, List<CustomActivityModel.DetailReward> rewards)
        {
            UnityEngine.Component button = tier == 1 ? _btn_1 : tier == 2 ? _btn_2 : _btn_3;
            TMPro.TextMeshProUGUI label = tier == 1 ? _lb_btn_1 : tier == 2 ? _lb_btn_2 : _lb_btn_3;
            UnityEngine.Component red = tier == 1 ? _btn_red_1 : tier == 2 ? _btn_red_2 : _btn_red_3;
            if (button != null) button.gameObject.SetActive(active);
            int gold = 0;
            bool claim = false;
            foreach (CustomActivityModel.DetailReward reward in rewards)
            {
                if (TierOf(reward) != tier) continue;
                if (gold == 0) gold = ActivityViewUtil.ConditionInt(reward.Condition, "gold");
                if (reward.Status == 1) claim = true;
            }
            if (label != null) label.text = gold > 0 ? gold / 10 + "元" : "档位" + tier;
            if (red != null) red.gameObject.SetActive(claim);
        }

        private static int TierOf(CustomActivityModel.DetailReward reward)
        {
            return Mathf.Clamp(ActivityViewUtil.ConditionInt(reward.Condition, "tier", 1), 1, 3);
        }

        private void HideSharedTemplates()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_RevelationDevourItem != null) _tpl_RevelationDevourItem.SetActive(false);
            if (_tpl_TeamHallItem != null) _tpl_TeamHallItem.SetActive(false);
            if (_tpl_TopVipShopItem != null) _tpl_TopVipShopItem.SetActive(false);
        }
    }
}
