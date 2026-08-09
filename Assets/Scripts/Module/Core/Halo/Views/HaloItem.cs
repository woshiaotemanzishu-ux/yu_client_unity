using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Halo;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Halo
{
    /// <summary>
    /// 光环特权列表项。只读呈现 51400 快照；51401 领取属于账号写事务，本轮点击保持 blocked、绝不发包。
    /// 三个奖励格统一实例化 Common/BaseAwardItem，不复制共享格子节点树。
    /// </summary>
    public sealed class HaloItem : HaloItemBind
    {
        private const float RewardCellSize = 56f;
        private const float RewardCellStep = 67f;

        private readonly List<GameObject> _rewardCells = new List<GameObject>();
        private HaloConfigs.Entry _entry;
        private int _renderEpoch;

        protected override void OnInit()
        {
            BindClick(img_get, OnClaimClicked);
            BindClick(img_mask, OnMaskClicked);
            BindClick(box_desc, OnDescriptionClicked);
        }

        public void SetData(HaloConfigs.Entry entry)
        {
            _entry = entry;
            if (entry == null) return;

            _ = ResManager.SetImageAsync(img_bg, GameResPath.GetIconOtherPath("halo", entry.Picture), false, false);
            SetNode(img_flag, false); // 老端仅首次新解锁 ConsolidateData 标记；本端无等价持久快照，不猜测。
            SetNode(box_desc, !string.IsNullOrEmpty(entry.SupplementDescription));
            RefreshState();
            RenderRewards(entry);
        }

        public void RefreshState()
        {
            if (_entry == null) return;
            bool purchased = HaloModel.Instance.EndTime > 0;
            bool unlocked = IsConditionMet(_entry);
            int state = HaloModel.Instance.GetRewardState(_entry.Id);

            SetNode(img_mask, !purchased || !unlocked);
            SetNode(img_get, purchased && unlocked && state != 1);
            SetNode(img_getted, purchased && unlocked && state == 1);
            if (lable_mask_desc != null)
            {
                lable_mask_desc.text = _entry.ConditionType == "task"
                    ? "任务" + _entry.ConditionValue
                    : _entry.ConditionValue + "级";
            }
        }

        protected override void OnDispose()
        {
            ClearRewardCells();
            base.OnDispose();
        }

        private void OnDestroy() => ClearRewardCells();

        private async void RenderRewards(HaloConfigs.Entry entry)
        {
            ClearRewardCells();
            int epoch = ++_renderEpoch;
            if (box_parent == null) return;
            for (int i = 0; i < entry.Rewards.Count; i++)
            {
                HaloConfigs.Reward reward = entry.Rewards[i];
                GameObject go = await ResManager.InstantiateAsync(
                    GameResPath.GetUIPrefab("common", "BaseAwardItem"), box_parent);
                if (this == null || epoch != _renderEpoch || _entry != entry)
                {
                    if (go != null) ResManager.ReleaseInstance(go);
                    return;
                }
                if (go == null) continue;

                go.SetActive(true);
                RectTransform rect = go.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(i * RewardCellStep, 0f);
                }
                BaseAwardItem cell = go.GetComponent<BaseAwardItem>();
                if (cell != null)
                {
                    (int goodsId, int locked) = GoodsModel.GetMappingTypeId(reward.Type, reward.TypeId);
                    cell.SetScale(RewardCellSize / 127f);
                    cell.SetData(goodsId, reward.Count, locked != 0);
                }
                _rewardCells.Add(go);
            }
        }

        private static bool IsConditionMet(HaloConfigs.Entry entry)
        {
            if (entry.ConditionType == "lv") return RoleModel.Instance.Level >= entry.ConditionValue;
            if (entry.ConditionType == "task")
                return TaskModel.Instance.NewestFinishTaskId >= entry.ConditionValue;
            return false;
        }

        private void OnClaimClicked()
        {
            if (_entry == null) return;
            if (HaloModel.Instance.GetRewardState(_entry.Id) == 1)
            {
                TipsManager.Toast("已领取");
                return;
            }
            GameLog.Warn("Halo", "blocked: 51401 未发送，领取光环奖励属于未授权账号写事务。id={0}", _entry.Id);
        }

        private void OnMaskClicked()
        {
            if (_entry == null) return;
            string tip;
            if (HaloModel.Instance.EndTime <= 0)
                tip = _entry.ConditionValue <= 1 ? "购买激活" : "购买并达到" + _entry.ConditionValue + "级激活";
            else
                tip = _entry.ConditionType == "task" ? "完成指定任务后激活" : _entry.ConditionValue + "级激活";
            TipsManager.Toast(tip);
        }

        private void OnDescriptionClicked()
        {
            if (_entry == null || string.IsNullOrEmpty(_entry.SupplementDescription)) return;
            GameLog.Warn("Halo",
                "blocked: TeamSmallDescView 未在 Unity 落地且属于 Common 跨岛共享弹窗，本岛不以 Toast 冒充。id={0}",
                _entry.Id);
        }

        private void ClearRewardCells()
        {
            ++_renderEpoch;
            for (int i = 0; i < _rewardCells.Count; i++)
                if (_rewardCells[i] != null) ResManager.ReleaseInstance(_rewardCells[i]);
            _rewardCells.Clear();
        }

        private static void BindClick(Component target, System.Action callback)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, callback);
        }

        private static void SetNode(Component node, bool visible)
        {
            if (node != null) node.gameObject.SetActive(visible);
        }
    }
}
