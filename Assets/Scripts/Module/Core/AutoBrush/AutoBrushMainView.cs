using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.AutoBrush;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.AutoBrush
{
    public sealed class AutoBrushMainView
    {
        private readonly AutoBrushMainViewBind _bind;
        private readonly List<GameObject> _rewardCells = new List<GameObject>();
        private int _renderEpoch;
        private int _showLevel;
        private bool _eventsBound;

        public AutoBrushMainView(AutoBrushMainViewBind bind)
        {
            _bind = bind;
            if (_bind?._tpl_EquipmentItem != null) _bind._tpl_EquipmentItem.SetActive(false);
        }

        public bool IsShown => _bind != null && _bind.gameObject.activeSelf;

        public void Show()
        {
            if (_bind == null) return;
            _bind.Show();
            _bind.transform.SetAsLastSibling();
            BindEvents();
            BindClicks();
            _ = RenderAsync();
        }

        public void Hide()
        {
            UnbindEvents();
            ClearRewardCells();
            if (_bind != null) _bind.Hide();
        }

        private void BindEvents()
        {
            if (_eventsBound) return;
            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_INFO_UPDATED, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_STATE_UPDATED, Refresh);
            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound) return;
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_INFO_UPDATED, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_STATE_UPDATED, Refresh);
            _eventsBound = false;
        }

        private void Refresh()
        {
            if (!IsShown) return;
            _ = RenderAsync();
        }

        private async Task RenderAsync()
        {
            int epoch = ++_renderEpoch;
            await AutoBrushConfigs.EnsureLoaded();
            await GoodsModel.EnsureLoaded();
            if (epoch != _renderEpoch || _bind == null || !IsShown) return;

            AutoBrushModel model = AutoBrushModel.Instance;
            bool done = model.CheckDoneState();
            _showLevel = done ? model.Level : model.Level + 1;

            if (_bind._lb_level != null) _bind._lb_level.text = "第" + _showLevel + "关";
            if (_bind._html_exp != null) _bind._html_exp.text = AutoBrushConfigs.BuildExpText(_showLevel, done);
            if (_bind._lb_first_level != null) _bind._lb_first_level.text = model.TopRankLevel > 0 ? model.TopRankLevel + "关" : "暂无排名";
            if (_bind._lb_first_name != null) _bind._lb_first_name.text = model.TopRankName;
            if (_bind._lb_level_reward != null) _bind._lb_level_reward.text = "";
            if (_bind._lb_go != null) _bind._lb_go.text = "前往";

            RefreshBrushInfo();
            await BuildRewardCells(epoch, AutoBrushConfigs.BuildBossRewards(_showLevel, RoleModel.Instance.Career));
        }

        private void RefreshBrushInfo()
        {
            AutoBrushModel.BrushStrangeInfo info = AutoBrushModel.Instance.BrushInfo;
            bool hasInfo = info != null && info.NeedTimes > 0;
            bool ready = hasInfo && info.CurrentTimes >= info.NeedTimes;

            SetActive(_bind._box_enter, hasInfo && ready);
            SetActive(_bind._img_enter_red, hasInfo && ready);
            SetActive(_bind._html_desc, hasInfo && !ready);
            SetActive(_bind._lb_go, hasInfo && !ready);
            SetActive(_bind._box_assist, false);
            SetActive(_bind._img_assist_tips, false);
            SetActive(_bind._box_show_effect, false);
            SetActive(_bind._img_show_red, false);

            if (_bind._html_desc != null)
            {
                int left = hasInfo ? Mathf.Max(0, info.NeedTimes - info.CurrentTimes) : 0;
                _bind._html_desc.text = hasInfo ? "还需要击败" + left + "个野外妖怪才可闯关" : "";
            }
            if (_bind._lb_enter != null) _bind._lb_enter.text = "挑战";
        }

        private async Task BuildRewardCells(int epoch, IReadOnlyList<AutoBrushModel.RewardEntry> rewards)
        {
            ClearRewardCells(false);
            if (_bind?._panel_reward == null || _bind._tpl_EquipmentItem == null) return;
            if (rewards == null || rewards.Count == 0)
            {
                _bind._panel_reward.gameObject.SetActive(false);
                return;
            }

            _bind._panel_reward.gameObject.SetActive(true);
            Transform parent = _bind._panel_reward.content != null
                ? _bind._panel_reward.content
                : _bind._panel_reward.transform;

            for (int i = 0; i < rewards.Count; i++)
            {
                AutoBrushModel.RewardEntry reward = rewards[i];
                (int goodsId, int locked) = GoodsModel.GetMappingTypeId(reward.Style, reward.RawTypeId);
                if (goodsId <= 0) goodsId = reward.RawTypeId;
                if (goodsId <= 0) continue;

                GameObject cellGo = Object.Instantiate(_bind._tpl_EquipmentItem, parent);
                if (epoch != _renderEpoch)
                {
                    Object.Destroy(cellGo);
                    return;
                }

                cellGo.SetActive(true);
                EquipmentItem cell = cellGo.GetComponent<EquipmentItem>();
                if (cell == null)
                {
                    Object.Destroy(cellGo);
                    continue;
                }

                cell.Show();
                RectTransform rt = (RectTransform)cellGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(i * 108f, 0f);
                cell.SetScale(0.65f);
                cell.SetData(goodsId, reward.Count, locked > 0);
                _rewardCells.Add(cellGo);

                await Task.Yield();
                if (epoch != _renderEpoch) return;
            }
        }

        private void BindClicks()
        {
            BindClick(_bind._box_enter, EnterDungeon);
            BindClick(_bind._img_enter, EnterDungeon);
            BindClick(_bind._lb_go, GoBrushMonster);
            BindClick(_bind._img_rank, () => GameLog.Info("AutoBrush", "AutoBrushRankView not migrated yet"));
            BindClick(_bind._box_assist, () => GameLog.Info("AutoBrush", "guild assist not migrated yet"));
        }

        private void EnterDungeon()
        {
            AutoBrushModel.BrushStrangeInfo info = AutoBrushModel.Instance.BrushInfo;
            if (info == null || info.NeedTimes <= 0) return;
            if (info.CurrentTimes < info.NeedTimes)
            {
                GameLog.Info("AutoBrush", "enter blocked progress={0}/{1}", info.CurrentTimes, info.NeedTimes);
                return;
            }

            // RequestEnterOrExit 会先进入 Entering：收脚、清目标并冻结；Boss 入场演出结束后才放行自动战斗。
            AutoBrushController.Instance.RequestEnterOrExit(0);
            Hide();
        }

        private void GoBrushMonster()
        {
            if (AutoBrushModel.Instance.AutoBrushState)
            {
                StartFieldAutoFight();
            }
            else
            {
                AutoBrushController.Instance.RequestAutoBrushState(true);
            }
            Hide();
        }

        private static void StartFieldAutoFight()
        {
            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (task?.TaskTipsType == TaskModel.TIP_PASS_MAIN_DUNGEON)
            {
                bool resumed = TaskModel.Instance.ResumeCurrentTaskAutoFight();
                if (!resumed) AutoFightModel.Instance.SetAutoFightWeight(AutoFightModel.AUTO_WEIGHT_TASK);
                GameLog.Info("AutoBrush", "go field auto-fight for PassMainDungeon resumed={0}", resumed);
                return;
            }

            AutoFightModel.Instance.SetAutoFight(true);
            GameLog.Info("AutoBrush", "go field auto-fight");
        }

        private static void BindClick(Component target, System.Action action)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(target, action);
        }

        private static void SetActive(Component comp, bool active)
        {
            if (comp != null) comp.gameObject.SetActive(active);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private void ClearRewardCells(bool bumpEpoch = true)
        {
            if (bumpEpoch) _renderEpoch++;
            for (int i = 0; i < _rewardCells.Count; i++)
            {
                if (_rewardCells[i] != null) Object.Destroy(_rewardCells[i]);
            }
            _rewardCells.Clear();
        }
    }
}
