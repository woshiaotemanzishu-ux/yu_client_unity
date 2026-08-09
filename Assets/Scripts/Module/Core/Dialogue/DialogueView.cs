using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Dialogue;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Dialogue
{
    /// <summary>
    /// Runtime controller for the converted dialogue prefab.
    /// Static layout/skins come from DialogueModule.prefab; this class only fills data,
    /// binds clicks, drives the NPC model, and mirrors the old client's auto countdown.
    /// </summary>
    public sealed class DialogueView
    {
        private const int AutoSeconds = 10;
        private GameObject _moduleRoot;
        private DialogueViewBind _bind;
        private Task<bool> _loadTask;
        private CancellationTokenSource _timerCts;
        private Action _timerAction;
        private Graphic _clickSurface;
        private Action _currentClickAction;
        private readonly List<GameObject> _rewardCells = new List<GameObject>();

        private NpcDialogVo _vo;
        private int _dialogIndex;
        private int _openEpoch;
        private int _modelEpoch;
        private int _rewardEpoch;
        private int _time;
        private bool _layersHidden;
        private bool _actionConsumed;
        private bool _mainLayerWasActive;
        private bool _popupLayerWasActive;

        private const float MODEL_BASE_SCALE = 0.675f;
        private const float MODEL_YAW = 175f;
        private static readonly Vector2 MODEL_POS = new Vector2(0.5f, 0.25f);

        public void Open(NpcDialogVo vo)
        {
            if (vo == null) return;
            _vo = vo;
            _dialogIndex = 1;
            _ = OpenAsync(++_openEpoch);
        }

        public void Close()
        {
            ++_openEpoch;
            CancelTimer();
            _currentClickAction = null;
            _actionConsumed = true;
            ClearRewardCells();
            _modelEpoch++;
            UIModelStage.Clear();

            if (_bind != null) _bind.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
            RestoreMainLayers();

            DialogueModel.Instance.DialogIsOpen = false;
            DialogueModel.Instance.CurrentNpcId = 0;
            GameLog.Info("Dialogue", "dialogue view closed");
        }

        private async Task OpenAsync(int epoch)
        {
            if (!await EnsureLoaded()) return;
            if (epoch != _openEpoch || _vo == null) return;

            _moduleRoot.SetActive(true);
            _bind.Show();
            _bind.transform.SetAsLastSibling();
            SetMainLayersVisible(false);

            DialogueModel.Instance.DialogIsOpen = true;
            DialogueModel.Instance.CurrentNpcId = _vo.NpcId;

            Render();
            _ = ShowNpcModel(_vo.NpcId);
            GameLog.Info("Dialogue", "dialogue view opened: npcId={0} talkId={1} tasks={2} default={3}",
                _vo.NpcId, _vo.TalkId, _vo.TaskList.Count, _vo.IsDefaultTalk());
        }

        private async Task<bool> EnsureLoaded()
        {
            if (_bind != null && _moduleRoot != null) return true;
            if (_loadTask == null) _loadTask = LoadPrefab();
            Task<bool> task = _loadTask;
            bool loaded = await task;
            if (!loaded && ReferenceEquals(_loadTask, task)) _loadTask = null;
            return loaded;
        }

        private async Task<bool> LoadPrefab()
        {
            // 老端位于 Message 层并只隐藏 Main/Activity；Unity 以 Tip 承接 Message，
            // 这样才能在隐藏 Popup(Activity) 后仍保持对话本身可见。
            Transform parent = ViewManager.GetLayer(UILayer.Tip);
            if (parent == null)
            {
                GameLog.Error("Dialogue", "cannot load DialogueModule: Tip layer is missing");
                return false;
            }

            string key = GameResPath.GetUIPrefab("dialogue", "DialogueModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, parent);
            if (_moduleRoot == null)
            {
                GameLog.Error("Dialogue", "DialogueModule prefab load failed: {0}", key);
                return false;
            }

            _moduleRoot.name = "DialogueModule";
            BaseView[] views = _moduleRoot.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views) v.gameObject.SetActive(false);

            _bind = _moduleRoot.GetComponentInChildren<DialogueViewBind>(true);
            if (_bind == null)
            {
                GameLog.Error("Dialogue", "DialogueModule missing DialogueViewBind. Run dialogue LayaUI convert + bind backfill.");
                ResManager.ReleaseInstance(_moduleRoot);
                _moduleRoot = null;
                return false;
            }
            if (_bind._tpl_EquipmentItem != null) _bind._tpl_EquipmentItem.SetActive(false);
            ConfigureUniversalClickSurface();

            _moduleRoot.SetActive(false);
            return true;
        }

        private void Render()
        {
            if (_vo == null || _bind == null) return;
            CancelTimer();
            _currentClickAction = null;
            _actionConsumed = false;
            BindStaticClicks();

            if (_bind._lb_name != null) _bind._lb_name.text = DialogueModel.Instance.GetNpcName(_vo.NpcId);

            TalkConfigs.TalkContentBlock block = _vo.GetContent(_dialogIndex);
            var lines = new List<string>();
            var actionNodes = new List<DialogueNodeVo>();
            if (block != null)
            {
                foreach (DialogueNodeVo n in block.Nodes)
                {
                    if (DialogueTypeConst.IsActionNode(n.Type)) actionNodes.Add(n);
                    else if (!string.IsNullOrEmpty(n.Text)) lines.Add(n.Text);
                }
            }

            string body = lines.Count > 0
                ? string.Join("\n", lines)
                : (_vo.TalkCfg == null ? "(talk_id=" + _vo.TalkId + " has no config text)" : "");
            if (_bind._lb_content != null) _bind._lb_content.text = body;
            if (_bind._lb_award != null) _bind._lb_award.text = "";

            bool hasReward = _vo.Rewards != null && _vo.Rewards.Count > 0;
            SetActive(_bind._box_award_con, hasReward);
            _ = BuildRewardCells(_vo.Rewards);

            if (actionNodes.Count > 0)
            {
                // 老端逐项覆盖 event_text，最终执行同一内容块里的最后一个动作节点。
                DialogueNodeVo node = actionNodes[actionNodes.Count - 1];
                bool useRewardPanel = UseRewardGetPanel(node.Type);
                if (useRewardPanel && _bind._lb_award != null) _bind._lb_award.text = body;
                ShowGetButton(ActionDefaultText(node.Type), () =>
                {
                    DialogueController.Instance.ClickAnswerHandler(_vo, node);
                    Close();
                }, useRewardPanel);
                return;
            }

            if (_vo.TaskList.Count > 0)
            {
                DialogueTaskEntry task = _vo.TaskList[0];
                if (_bind._lb_award != null) _bind._lb_award.text = body;
                ShowGetButton(TaskActionText(task.TaskState), () =>
                {
                    DialogueController.Instance.SelectTask(_vo.NpcId, task.TaskId, task.TaskState);
                }, true);
                return;
            }

            if (_dialogIndex < _vo.GetTalkContentCount())
            {
                ShowContinueButton(NextDialog);
                return;
            }

            ShowContinueButton(Close);
        }

        private void BindStaticClicks()
        {
            if (_bind._lb_skip != null) _bind._lb_skip.text = "跳过";
        }

        private void ShowGetButton(string text, Action onClick, bool isTaskFinish)
        {
            SetActive(_bind._box_dialog_con, !isTaskFinish);
            SetActive(_bind._box_get_con, isTaskFinish);
            SetActive(_bind._box_go_on, false);
            SetActive(_bind._box_skip, true);

            if (_bind._lb_get_click != null) _bind._lb_get_click.text = text;
            _currentClickAction = onClick;

            StartAutoTimer(isTaskFinish, TriggerCurrentClick, AutoSeconds);
        }

        private void ShowContinueButton(Action onClick)
        {
            SetActive(_bind._box_dialog_con, true);
            SetActive(_bind._box_get_con, false);
            SetActive(_bind._box_go_on, true);
            SetActive(_bind._box_skip, true);

            if (_bind._lb_go_on_desc != null) _bind._lb_go_on_desc.text = "继续";
            _currentClickAction = onClick;

            StartAutoTimer(false, TriggerCurrentClick, AutoSeconds);
        }

        /// <summary>
        /// 老端把全屏背景、底部面板、继续、领取和跳过都汇总到 OnTaskSelect。
        /// Unity 复用 Prefab 内已经铺满屏幕的 _img_bg 作为唯一点击面，子 Graphic 仅负责显示，
        /// 避免同一手势执行两次，也不在运行时给人工 Prefab 根节点追加 Image。
        /// </summary>
        private void ConfigureUniversalClickSurface()
        {
            if (_moduleRoot == null || _bind == null) return;

            _clickSurface = _bind._img_bg;
            if (_clickSurface == null)
            {
                GameLog.Error("Dialogue", "DialogueModule missing full-screen _img_bg click surface");
                return;
            }
            _clickSurface.raycastTarget = true;
            UIUtil.AddClick(_clickSurface, TriggerCurrentClick);
            foreach (Graphic graphic in _moduleRoot.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != null && graphic != _clickSurface) graphic.raycastTarget = false;
            }
        }

        private void TriggerCurrentClick()
        {
            if (_actionConsumed || _currentClickAction == null) return;

            _actionConsumed = true;
            Action action = _currentClickAction;
            _currentClickAction = null;
            CancelTimer();
            action.Invoke();
        }

        private void NextDialog()
        {
            _dialogIndex++;
            if (_vo == null || _dialogIndex > _vo.GetTalkContentCount())
            {
                Close();
                return;
            }
            Render();
        }

        private async Task BuildRewardCells(IReadOnlyList<TaskReward.Entry> rewards)
        {
            int epoch = ++_rewardEpoch;
            ClearRewardCells(false);
            if (_bind == null || _bind._box_award_con == null || _bind._tpl_EquipmentItem == null) return;
            if (rewards == null || rewards.Count == 0) return;

            for (int i = 0; i < rewards.Count; i++)
            {
                GameObject cellGo = UnityEngine.Object.Instantiate(_bind._tpl_EquipmentItem, _bind._box_award_con);
                if (epoch != _rewardEpoch)
                {
                    UnityEngine.Object.Destroy(cellGo);
                    return;
                }
                cellGo.SetActive(true);

                EquipmentItem cell = cellGo.GetComponent<EquipmentItem>();
                if (cell == null)
                {
                    GameLog.Warn("Dialogue", "EquipmentItem template missing EquipmentItem component. Run dialogue/common bind backfill.");
                    UnityEngine.Object.Destroy(cellGo);
                    continue;
                }

                cell.Show();
                RectTransform rt = (RectTransform)cellGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(i * 90f, 0f);
                cell.SetScale(0.6f);
                cell.SetData(rewards[i].TypeId, rewards[i].Count);
                foreach (Graphic graphic in cellGo.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
                _rewardCells.Add(cellGo);

                await Task.Yield();
                if (epoch != _rewardEpoch) return;
            }
        }

        private void StartAutoTimer(bool showFinishText, Action action, int seconds)
        {
            ClearTimerLabels();
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;

            CancelTimer();
            _timerAction = action;
            _time = seconds > 0 ? seconds : AutoSeconds;
            _timerCts = new CancellationTokenSource();
            _ = AutoTimerLoop(showFinishText, _timerCts.Token);
        }

        private async Task AutoTimerLoop(bool showFinishText, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _time--;
                if (_time > 0)
                {
                    if (showFinishText)
                    {
                        if (_bind?._lb_get_time != null) _bind._lb_get_time.text = _time + "秒后继续";
                    }
                    else
                    {
                        if (_bind?._lb_go_on_time != null) _bind._lb_go_on_time.text = _time.ToString();
                    }

                    try { await Shenxiao.Framework.Util.TimeUtil.Delay(1000, token); }
                    catch (TaskCanceledException) { return; }
                    continue;
                }

                Action action = _timerAction;
                CancelTimer();
                action?.Invoke();
                return;
            }
        }

        private void CancelTimer()
        {
            if (_timerCts != null)
            {
                _timerCts.Cancel();
                _timerCts.Dispose();
                _timerCts = null;
            }
            _timerAction = null;
            ClearTimerLabels();
        }

        private void ClearTimerLabels()
        {
            if (_bind?._lb_get_time != null) _bind._lb_get_time.text = "";
            if (_bind?._lb_go_on_time != null) _bind._lb_go_on_time.text = "";
        }

        private void ClearRewardCells(bool bumpEpoch = true)
        {
            if (bumpEpoch) _rewardEpoch++;
            for (int i = 0; i < _rewardCells.Count; i++)
            {
                if (_rewardCells[i] != null) UnityEngine.Object.Destroy(_rewardCells[i]);
            }
            _rewardCells.Clear();
        }

        private async Task ShowNpcModel(int npcId)
        {
            if (_bind?._box_model == null) return;
            int epoch = ++_modelEpoch;

            await NpcConfigs.EnsureLoaded();
            if (epoch != _modelEpoch) return;

            NpcConfigs.NpcCfg cfg = NpcConfigs.Get(npcId);
            string modelKey = NpcConfigs.GetModelKey(npcId, cfg, out string modelModule, out string modelResId);
            GameObject prefab = await ResManager.LoadAsync<GameObject>(modelKey);
            if (epoch != _modelEpoch || _moduleRoot == null || !_moduleRoot.activeSelf) return;

            if (prefab == null)
            {
                UIModelStage.Clear();
                GameLog.Warn("Dialogue", "dialogue model missing: npcId={0} key={1}", npcId, modelKey);
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            float talkScale = cfg != null && cfg.TalkScale > 0f ? cfg.TalkScale : 1f;
            UIModelStage.ShowInstance(_bind._box_model, instance, MODEL_BASE_SCALE * talkScale, MODEL_POS, MODEL_YAW);
            await PlayNpcIdle(instance, modelModule, modelResId);
        }

        private static async Task PlayNpcIdle(GameObject model, string module, string resId)
        {
            if (model == null) return;
            string actionKey = NpcConfigs.GetActionKey(module, resId, "idle");
            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>(actionKey);
            if (model == null || clip == null) return;

            Animation anim = model.GetComponent<Animation>();
            if (anim == null) anim = model.AddComponent<Animation>();
            if (anim.GetClip("idle") == null) anim.AddClip(clip, "idle");
            anim.Play("idle");
        }

        private static bool IsFinishAction(int type)
        {
            return type == DialogueTypeConst.FINISH || type == DialogueTypeConst.FINISH_AND_TRIGGER;
        }

        private static bool UseRewardGetPanel(int type)
        {
            return type == DialogueTypeConst.FINISH
                   || type == DialogueTypeConst.FINISH_AND_TRIGGER
                   || type == DialogueTypeConst.TRIGGER_AND_FINISH
                   || type == DialogueTypeConst.TALK_EVENT;
        }

        private static string ActionDefaultText(int type)
        {
            switch (type)
            {
                case DialogueTypeConst.TALK_EVENT:
                    return "接取任务";
                case DialogueTypeConst.TRIGGER:
                case DialogueTypeConst.FINISH:
                case DialogueTypeConst.TRIGGER_AND_FINISH:
                case DialogueTypeConst.FINISH_AND_TRIGGER:
                    return "完成任务";
                default:
                    return "确定";
            }
        }

        private static string TaskActionText(int state)
        {
            switch (state)
            {
                case 1: return "接取任务";
                case 3: return "完成任务";
                default: return "继续";
            }
        }

        private static void SetActive(Component c, bool active)
        {
            if (c != null) c.gameObject.SetActive(active);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private void SetMainLayersVisible(bool visible)
        {
            Transform main = ViewManager.GetLayer(UILayer.Main);
            Transform popup = ViewManager.GetLayer(UILayer.Popup);
            if (!_layersHidden)
            {
                _mainLayerWasActive = main == null || main.gameObject.activeSelf;
                _popupLayerWasActive = popup == null || popup.gameObject.activeSelf;
                _layersHidden = true;
            }
            if (main != null) main.gameObject.SetActive(visible);
            if (popup != null) popup.gameObject.SetActive(visible);
        }

        private void RestoreMainLayers()
        {
            if (!_layersHidden) return;
            Transform main = ViewManager.GetLayer(UILayer.Main);
            Transform popup = ViewManager.GetLayer(UILayer.Popup);
            if (main != null) main.gameObject.SetActive(_mainLayerWasActive);
            if (popup != null) popup.gameObject.SetActive(_popupLayerWasActive);
            _layersHidden = false;
        }
    }
}
