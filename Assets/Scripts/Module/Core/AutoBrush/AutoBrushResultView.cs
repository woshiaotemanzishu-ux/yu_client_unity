using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Common.Audio;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.AutoBrush;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Shenxiao.Module.Core.AutoBrush
{
    /// <summary>
    /// Runtime presenter for converted AutoBrushResultView.
    /// Static layout comes from AutoBrushModule.prefab; this class only fills protocol data.
    /// </summary>
    public sealed class AutoBrushResultView
    {
        private const int AutoCloseSeconds = 10;

        private readonly AutoBrushResultViewBind _bind;
        private readonly List<GameObject> _rewardCells = new List<GameObject>();
        private CancellationTokenSource _timerCts;
        private IReadOnlyList<AutoBrushModel.RewardEntry> _rewards;
        private int _coin;
        private int _exp;
        private int _rewardEpoch;
        private int _leftTime;
        private Action _onCompleted;

        public AutoBrushResultView(AutoBrushResultViewBind bind)
        {
            _bind = bind;
            if (_bind?._tpl_EquipmentItem != null) _bind._tpl_EquipmentItem.SetActive(false);
        }

        public bool IsShown => _bind != null && _bind.gameObject.activeSelf;

        public void Show(IReadOnlyList<AutoBrushModel.RewardEntry> rewards, int coin, int exp, Action onCompleted)
        {
            if (_bind == null) return;
            _rewards = rewards ?? new List<AutoBrushModel.RewardEntry>();
            _coin = coin;
            _exp = exp;
            _onCompleted = onCompleted;
            _bind.Show();
            _ = AudioManager.PlayFightingVoice(RoleModel.Instance.Sex, 2);
            _bind.transform.SetAsLastSibling();
            Render();
            StartTimer();
        }

        public void Hide()
        {
            CancelTimer();
            _onCompleted = null;
            ClearRewardCells();
            if (_bind != null) _bind.Hide();
        }

        private void Render()
        {
            if (_bind == null) return;
            if (_bind._lb_old_exp != null) _bind._lb_old_exp.text = "";
            if (_bind._lb_old_coin != null) _bind._lb_old_coin.text = "";
            if (_bind._lb_now_exp != null) _bind._lb_now_exp.text = _exp > 0 ? _exp.ToString() : "0";
            if (_bind._lb_now_coin != null) _bind._lb_now_coin.text = _coin > 0 ? _coin.ToString() : "0";
            if (_bind._lb_exit != null) _bind._lb_exit.text = "完成";

            BindClick(_bind._box_exit, Complete);
            BindClick(_bind._img_exit, Complete);
            _ = BuildRewardCells(_rewards);
        }

        private static void BindClick(Component target, System.Action action)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(target, action);
        }

        private async Task BuildRewardCells(IReadOnlyList<AutoBrushModel.RewardEntry> rewards)
        {
            int epoch = ++_rewardEpoch;
            ClearRewardCells(false);
            if (_bind == null || _bind._panel_reward == null || _bind._tpl_EquipmentItem == null) return;
            if (rewards == null || rewards.Count == 0) return;

            await GoodsModel.EnsureLoaded();
            if (epoch != _rewardEpoch || _bind == null) return;

            Transform parent = _bind._panel_reward.content != null
                ? _bind._panel_reward.content
                : _bind._panel_reward.transform;

            for (int i = 0; i < rewards.Count; i++)
            {
                GameObject cellGo = Object.Instantiate(_bind._tpl_EquipmentItem, parent);
                if (epoch != _rewardEpoch)
                {
                    Object.Destroy(cellGo);
                    return;
                }

                cellGo.SetActive(true);
                EquipmentItem cell = cellGo.GetComponent<EquipmentItem>();
                if (cell == null)
                {
                    GameLog.Warn("AutoBrush", "AutoBrushResultView reward template missing EquipmentItem");
                    Object.Destroy(cellGo);
                    continue;
                }

                AutoBrushModel.RewardEntry reward = rewards[i];
                (int goodsId, int _) = GoodsModel.GetMappingTypeId(reward.Style, reward.RawTypeId);
                if (goodsId <= 0) goodsId = reward.RawTypeId;
                cell.Show();
                RectTransform rt = (RectTransform)cellGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(i * 108f, 0f);
                cell.SetScale(0.65f);
                cell.SetData(goodsId, reward.Count);
                _rewardCells.Add(cellGo);

                await Task.Yield();
                if (epoch != _rewardEpoch) return;
            }
        }

        private void StartTimer()
        {
            CancelTimer();
            _leftTime = AutoCloseSeconds;
            _timerCts = new CancellationTokenSource();
            _ = TimerLoop(_timerCts.Token);
        }

        private async Task TimerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_bind?._lb_exit != null) _bind._lb_exit.text = "完成(" + _leftTime + ")";
                try { await Shenxiao.Framework.Util.TimeUtil.Delay(1000, token); }
                catch (TaskCanceledException) { return; }

                _leftTime--;
                if (_leftTime <= 0)
                {
                    Complete();
                    return;
                }
            }
        }

        public void Complete()
        {
            if (!IsShown) return;
            Action callback = _onCompleted;
            _onCompleted = null;
            CancelTimer();
            ClearRewardCells();
            if (_bind != null) _bind.Hide();
            callback?.Invoke();
        }

        private void CancelTimer()
        {
            if (_timerCts == null) return;
            _timerCts.Cancel();
            _timerCts.Dispose();
            _timerCts = null;
        }

        private void ClearRewardCells(bool bumpEpoch = true)
        {
            if (bumpEpoch) _rewardEpoch++;
            for (int i = 0; i < _rewardCells.Count; i++)
            {
                if (_rewardCells[i] != null) Object.Destroy(_rewardCells[i]);
            }
            _rewardCells.Clear();
        }
    }
}
