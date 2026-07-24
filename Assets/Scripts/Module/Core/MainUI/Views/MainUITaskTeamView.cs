using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;
using Shenxiao.Module.Core.Team;
using Shenxiao.Module.Core.TempleAwaken;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Task/team HUD view. The static initial state mirrors old
    /// MainUITaskTeamView.LoadSuccess + SwitchView(Task); task rows are driven
    /// by the real TaskController 30000 chain.
    /// </summary>
    public sealed class MainUITaskTeamView : MainUITaskTeamViewBind
    {
        private static readonly Color TaskTabLightColor = ParseColor("#FFF7D6");
        private static readonly Color TaskTabDarkColor = ParseColor("#6CFFD3");
        private const int AutoTaskMaxLevel = 65;
        private const int AutoTaskStartDelayMs = 500;
        private const int AutoTaskPeriodMs = 10000;
        private const float TaskViewportWidth = 210f;
        private const float TaskViewportFullHeight = 214f;
        private const float TaskItemSpacing = 2f;

        private readonly List<MainUITaskItem> _taskItems = new List<MainUITaskItem>();
        private readonly List<TeamMainRoleItem> _teamItems = new List<TeamMainRoleItem>();
        private CancellationTokenSource _autoTaskCts;
        private CancellationTokenSource _guideCts;
        private MainUITaskItem _mainLineItem; // _box_main_line 内的主线任务条(复用 MainUITaskItem 渲染)
        private bool _clickBound;
        private bool _taskFolded;
        private bool _activityFolded;
        private bool _teamTabSelected; // 当前是否停在"队伍"页签(对标老端 now_type==Team,决定 _box_team 是否可见)
        private int _templeAwakenRenderVersion;
        private int _templeAwakenRoleLevel = int.MinValue;

        protected override void OnInit()
        {
            _box_task.gameObject.SetActive(true);
            _box_team.gameObject.SetActive(false);
            _box_non_team.gameObject.SetActive(false);
            _img_team_red.gameObject.SetActive(false);
            _img_team_role_count_bg.gameObject.SetActive(false);
            _box_main_line.gameObject.SetActive(false);
            _panel_task.gameObject.SetActive(false);
            // 默认不展示 prefab 占位态；OnShow 收到 42901 后按真实配表和角色等级刷新。
            _box_temple_awaken.gameObject.SetActive(false);
            // 页签文字烤在 prefab 里("任务"/"组队"),代码不再改写 —— 想改文案去 prefab(所见即所得)。
            _lb_task_desc_en.gameObject.SetActive(false);
            _lb_team_desc_en.gameObject.SetActive(false);
            _img_awaken_red.gameObject.SetActive(false);
            _box_awaken_effect.gameObject.SetActive(false);
            if (_tpl_MainUITaskItem != null) _tpl_MainUITaskItem.SetActive(false);
            if (_tpl_TeamMainRoleItem != null) _tpl_TeamMainRoleItem.SetActive(false);
            ApplyTaskTabState(true);
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, RefreshTaskItems);
            EventDispatcher.On<int>(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskOneUpdated);
            EventDispatcher.On<int>(GlobalEvent.EVT_TASK_SELECT_CHANGED, OnTaskSelectChanged);
            EventDispatcher.On<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
            EventDispatcher.On(GlobalEvent.EVT_TEAM_INFO_UPDATE, RefreshTeamPanel);
            EventDispatcher.On(GlobalEvent.EVT_TEAM_APPLY_REDDOT_UPDATE, RefreshTeamRedDot);
            EventDispatcher.On(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE, RefreshTempleAwaken);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdateForTempleAwaken);
            RefreshTaskItems();
            RefreshTeamPanel();
            RefreshTeamRedDot();
            _templeAwakenRoleLevel = RoleModel.Instance.Level;
            RefreshTempleAwaken();
            StartAutoTaskTimer();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, RefreshTaskItems);
            EventDispatcher.Off<int>(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskOneUpdated);
            EventDispatcher.Off<int>(GlobalEvent.EVT_TASK_SELECT_CHANGED, OnTaskSelectChanged);
            EventDispatcher.Off<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
            EventDispatcher.Off(GlobalEvent.EVT_TEAM_INFO_UPDATE, RefreshTeamPanel);
            EventDispatcher.Off(GlobalEvent.EVT_TEAM_APPLY_REDDOT_UPDATE, RefreshTeamRedDot);
            EventDispatcher.Off(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE, RefreshTempleAwaken);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdateForTempleAwaken);
            _templeAwakenRenderVersion++;
            CancelGuideTimer();
            HideMainLineTaskArrow();
            CancelAutoTaskTimer();
        }

        /// <summary>太极折叠:整块任务/组队面板随太极收放。对标老端 LoadSuccess 里
        /// MainUIManager.AddMoveComponent(display_obj, MOVE_TYPE.Left)——太极一收就把整个 view 滑出左边,
        /// 收的是整块(含 _box_con 主体 + _img_arrow 收起箭头)。_img_arrow 是 _box_con 的兄弟、不在主体内,
        /// 故须单独一起收,否则会残留半折叠(箭头孤零零留在左边)。</summary>
        private void OnActivityFold(bool folded)
        {
            _activityFolded = folded;
            ApplyPanelFoldState();
        }

        private void OnTaskOneUpdated(int taskId)
        {
            RefreshTaskItems();
        }

        // 选中任务变化(点任务项后 DoTask 广播)→ 重刷任务栏,各项据 NowSelectTaskId 更新 _img_select 选中态。
        private void OnTaskSelectChanged(int taskId)
        {
            CancelGuideTimer();
            HideMainLineTaskArrow();
            RefreshTaskItems(false);
        }

        private void RefreshTaskItems()
        {
            RefreshTaskItems(true);
        }

        private void RefreshTaskItems(bool showMainLineGuide)
        {
            // 断线重连期 MainUI 可能已被拆(GameObject 销毁)但事件未退订 → _panel_task 为 Unity-null,
            // 在已销毁视图上刷新会抛 MissingReferenceException(阻断 DoTask 的 EVT_TASK_SELECT_CHANGED 链)。防御性早退。
            if (_panel_task == null) return;
            if (_taskFolded)
            {
                _panel_task.gameObject.SetActive(false);
                _box_main_line.gameObject.SetActive(false);
                CancelGuideTimer();
                HideMainLineTaskArrow();
                return;
            }

            List<TaskModel.TaskEntry> list = TaskModel.Instance.GetTaskListForMainUI();
            _panel_task.gameObject.SetActive(list.Count > 0);

            // 主线引导任务:老端在 _box_main_line 用 MainUITaskMainLineItem 渲染(带引导箭头/特效),
            // 并把主线从面板列表排除。Unity 暂无 MainUITaskMainLineItem 转换件 → 复用 MainUITaskItem 把
            // 真实主线任务渲到 _box_main_line(避免空槽 / 不留占位),再交给 MainUIGuideManager 显示旧端指引。
            TaskModel.TaskEntry mainLine = TaskModel.Instance.MainLineTaskNeedShowArrow()
                ? TaskModel.Instance.GetMainLineEntry() : null;
            RefreshMainLineItem(mainLine, showMainLineGuide);
            bool hasMainLine = mainLine != null && _mainLineItem != null;
            ApplyTaskViewport(hasMainLine, hasMainLine ? _mainLineItem.CurrentHeight : 0f);

            // 排版归 prefab:Content 上的 VerticalLayoutGroup 按 sibling 顺序竖排(间距/对齐去 prefab 调),
            // 这里只按顺序克隆/填数据/显隐,不再算坐标(原 -i*78 步进已删)。
            Transform parent = _panel_task.content != null ? _panel_task.content : _panel_task.transform;
            for (int i = 0; i < list.Count; i++)
            {
                MainUITaskItem item = GetOrCreateItem(i, parent);
                if (item == null) return;

                item.gameObject.SetActive(true);
                item.Show();
                item.SetData(list[i]);
            }

            for (int i = list.Count; i < _taskItems.Count; i++)
            {
                if (_taskItems[i] == null) continue;
                _taskItems[i].SetData(null);
                _taskItems[i].gameObject.SetActive(false);
            }
        }

        private MainUITaskItem GetOrCreateItem(int index, Transform parent)
        {
            if (index < _taskItems.Count && _taskItems[index] != null) return _taskItems[index];
            if (_tpl_MainUITaskItem == null)
            {
                GameLog.Error("MainUI", "MainUITaskItem template missing");
                return null;
            }

            GameObject go = Instantiate(_tpl_MainUITaskItem, parent);
            go.SetActive(true);
            MainUITaskItem item = go.GetComponent<MainUITaskItem>();
            if (item == null)
            {
                GameLog.Error("MainUI", "MainUITaskItem template is not rebound to business script");
                Destroy(go);
                return null;
            }

            while (_taskItems.Count <= index) _taskItems.Add(null);
            _taskItems[index] = item;
            return item;
        }

        // 主线任务条:有引导主线任务时显示在 _box_main_line(复用 MainUITaskItem);否则隐藏整槽。
        private void RefreshMainLineItem(TaskModel.TaskEntry entry, bool showGuide)
        {
            if (entry == null)
            {
                CancelGuideTimer();
                if (_mainLineItem != null)
                {
                    _mainLineItem.HideMainLineArrow();
                    _mainLineItem.gameObject.SetActive(false);
                }
                _box_main_line.gameObject.SetActive(false);
                return;
            }

            _box_main_line.gameObject.SetActive(true);
            MainUITaskItem item = EnsureMainLineItem();
            if (item == null) return;
            item.gameObject.SetActive(true);
            item.Show();
            item.SetData(entry);
            if (showGuide) ShowFinger();
            else item.HideMainLineArrow();
        }

        private MainUITaskItem EnsureMainLineItem()
        {
            if (_mainLineItem != null) return _mainLineItem;
            if (_tpl_MainUITaskItem == null)
            {
                GameLog.Error("MainUI", "MainUITaskItem template missing (main line)");
                return null;
            }

            GameObject go = Instantiate(_tpl_MainUITaskItem, _box_main_line);
            go.SetActive(true);
            _mainLineItem = go.GetComponent<MainUITaskItem>();
            if (_mainLineItem == null)
            {
                GameLog.Error("MainUI", "MainUITaskItem template not rebound (main line)");
                Destroy(go);
                return null;
            }
            // 贴 _box_main_line 左上(老端主线条在任务区顶部)。
            RectTransform rt = (RectTransform)_mainLineItem.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            return _mainLineItem;
        }

        /// <summary>对标老端 SetTaskConSize:无主线时任务列表占满 214px;有主线时从
        /// 「主线真实高度 + 2px」开始,剩余高度同步缩短。</summary>
        private void ApplyTaskViewport(bool hasMainLine, float mainLineHeight)
        {
            if (_panel_task == null) return;

            float top = hasMainLine ? mainLineHeight + TaskItemSpacing : 0f;
            float height = hasMainLine
                ? Mathf.Max(0f, TaskViewportFullHeight - top)
                : TaskViewportFullHeight;
            RectTransform viewport = (RectTransform)_panel_task.transform;
            viewport.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, TaskViewportWidth);
            viewport.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, height);
        }

        private void ShowFinger()
        {
            CancelGuideTimer();
            if (_mainLineItem == null
                || _box_main_line == null
                || !_box_main_line.gameObject.activeInHierarchy
                || !TaskModel.Instance.MainLineTaskNeedShowArrow())
            {
                HideMainLineTaskArrow();
                return;
            }

            _guideCts = new CancellationTokenSource();
            _ = ShowFingerDelayed(_guideCts.Token);
        }

        private async Task ShowFingerDelayed(CancellationToken token)
        {
            try
            {
                await Shenxiao.Framework.Util.TimeUtil.Delay(10, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;
            if (_mainLineItem == null
                || _box_main_line == null
                || !_box_main_line.gameObject.activeInHierarchy
                || !TaskModel.Instance.MainLineTaskNeedShowArrow())
            {
                HideMainLineTaskArrow();
                return;
            }

            _mainLineItem.ShowMainLineArrow();
        }

        private void CancelGuideTimer()
        {
            if (_guideCts == null) return;
            _guideCts.Cancel();
            _guideCts.Dispose();
            _guideCts = null;
        }

        private void HideMainLineTaskArrow()
        {
            if (_mainLineItem != null) _mainLineItem.HideMainLineArrow();
        }

        private void ApplyTaskTabState(bool taskSelected)
        {
            _ = ResManager.SetImageAsync(_img_task_bg,
                GameResPath.GetIcon("mainUI", taskSelected ? "mainui_taskbtn_light" : "mainui_taskbtn_normal"),
                nativeSize: false);
            _ = ResManager.SetImageAsync(_img_team_bg,
                GameResPath.GetIcon("mainUI", taskSelected ? "mainui_taskbtn_normal" : "mainui_taskbtn_light"),
                nativeSize: false);

            _lb_task_desc.color = taskSelected ? TaskTabLightColor : TaskTabDarkColor;
            _lb_team_desc.color = taskSelected ? TaskTabDarkColor : TaskTabLightColor;
        }

        private void BindButtons()
        {
            if (_clickBound) return;
            BindClick(_box_task_tab, ShowTaskTab);
            BindClick(_box_team_tab, ShowTeamTab);
            BindClick(_img_create_team, () => MainUIRouter.Open("team_create"));
            BindClick(_img_search_team, () => MainUIRouter.Open("team_search"));
            BindClick(_img_arrow, ToggleTaskPanel);
            BindClick(_box_temple_awaken, () => MainUIRouter.Open("templeawaken"));
            _clickBound = true;
        }

        private static void BindClick(Component target, Action onClick)
        {
            if (target == null) return;
            UIUtil.AddClick(target, onClick);
        }

        private void ShowTaskTab()
        {
            _teamTabSelected = false;
            _box_task.gameObject.SetActive(true);
            _box_team.gameObject.SetActive(false);
            _box_non_team.gameObject.SetActive(false);
            ApplyTaskTabState(true);
            RefreshTaskItems(false);
        }

        // 对标老端 Util.AddClickEvent(_box_team_tab,...) 先过 IsOpenTeam 门槛(主线101260前禁组队)。
        private void ShowTeamTab()
        {
            if (!TeamModel.Instance.IsOpenTeam(out string blockReason))
            {
                TipsManager.Toast(blockReason);
                return;
            }
            _teamTabSelected = true;
            _box_task.gameObject.SetActive(false);
            ApplyTaskTabState(false);
            RefreshTeamPanel();
            GameLog.Info("MainUI", "队伍页签点击 → 队伍大厅 TeamView 未移植,HUD 队伍区已接真实 TeamModel 数据");
        }

        /// <summary>对标老端 UpdateTeamData:有队伍 → 显 _box_team 并克隆 _tpl_TeamMainRoleItem 铺真实成员
        /// (人数不足 TEAMER_MAX 时补一个邀请空位,不铺满);无队伍 → 显 _box_non_team。
        /// _img_team_role_count_bg/_lb_team_role_count 无论当前在任务页还是队伍页都同步(对标老端无条件更新)。</summary>
        private void RefreshTeamPanel()
        {
            if (_box_team == null) return; // 断线重连期视图已拆,防御性早退(同 RefreshTaskItems)

            TeamModel model = TeamModel.Instance;
            bool hasTeam = model.HasTeam;
            if (_img_team_role_count_bg != null) _img_team_role_count_bg.gameObject.SetActive(hasTeam);
            if (hasTeam && _lb_team_role_count != null) _lb_team_role_count.text = model.Info.Members.Count.ToString();

            if (!_teamTabSelected) return;

            _box_non_team.gameObject.SetActive(!hasTeam);
            _box_team.gameObject.SetActive(hasTeam);
            if (!hasTeam) return;

            List<TeamModel.MemberVo> members = model.Info.Members;
            Transform parent = _list_team.content != null ? _list_team.content : _list_team.transform;
            int shown = 0;
            for (int i = 0; i < members.Count; i++)
            {
                TeamMainRoleItem item = GetOrCreateTeamItem(shown++, parent);
                if (item == null) return;
                item.gameObject.SetActive(true);
                item.Show();
                item.SetData(members[i]);
            }
            if (members.Count < TeamModel.TEAMER_MAX)
            {
                TeamMainRoleItem slot = GetOrCreateTeamItem(shown++, parent);
                if (slot != null)
                {
                    slot.gameObject.SetActive(true);
                    slot.Show();
                    slot.SetData(null); // 空位/邀请占位(对标老端补一个 {} 而非铺满剩余名额)
                }
            }
            for (int i = shown; i < _teamItems.Count; i++)
            {
                if (_teamItems[i] == null) continue;
                _teamItems[i].SetData(null);
                _teamItems[i].gameObject.SetActive(false);
            }
        }

        private TeamMainRoleItem GetOrCreateTeamItem(int index, Transform parent)
        {
            if (index < _teamItems.Count && _teamItems[index] != null) return _teamItems[index];
            if (_tpl_TeamMainRoleItem == null)
            {
                GameLog.Error("MainUI", "TeamMainRoleItem template missing");
                return null;
            }

            GameObject go = Instantiate(_tpl_TeamMainRoleItem, parent);
            go.SetActive(true);
            TeamMainRoleItem item = go.GetComponent<TeamMainRoleItem>();
            if (item == null)
            {
                GameLog.Error("MainUI", "TeamMainRoleItem template is not rebound to business script");
                Destroy(go);
                return null;
            }

            while (_teamItems.Count <= index) _teamItems.Add(null);
            _teamItems[index] = item;
            return item;
        }

        // 对标老端 _img_team_red(RedDotManager.TEAM_APPLY),读 TeamModel.HaveNewApply(24003 推送点亮)。
        private void RefreshTeamRedDot()
        {
            if (_img_team_red != null) _img_team_red.gameObject.SetActive(TeamModel.Instance.HaveNewApply);
        }

        private void ToggleTaskPanel()
        {
            _taskFolded = !_taskFolded;
            ApplyPanelFoldState();
            if (_taskFolded)
            {
                CancelGuideTimer();
                HideMainLineTaskArrow();
                GameLog.Info("MainUI", "任务/组队栏箭头点击 → 收起整个面板");
                return;
            }

            GameLog.Info("MainUI", "任务/组队栏箭头点击 → 展开整个面板");
            if (_teamTabSelected) RefreshTeamPanel();
            else RefreshTaskItems(false);
        }

        /// <summary>
        /// 独立箭头只收起 _box_con，箭头自身始终留在原位；太极全局折叠时才连箭头一起隐藏。
        /// 对标老端 switch_state：_box_con 滑出左侧、_img_arrow_icon.scaleX 翻转。
        /// </summary>
        private void ApplyPanelFoldState()
        {
            if (_box_con != null)
                _box_con.gameObject.SetActive(!_activityFolded && !_taskFolded);
            if (_img_arrow != null)
                _img_arrow.gameObject.SetActive(!_activityFolded);
            if (_img_arrow_icon != null)
            {
                Vector3 scale = _img_arrow_icon.rectTransform.localScale;
                float magnitude = Mathf.Abs(scale.x);
                if (magnitude < 0.001f) magnitude = 1f;
                scale.x = _taskFolded ? -magnitude : magnitude;
                _img_arrow_icon.rectTransform.localScale = scale;
            }
        }

        private void RefreshTempleAwaken()
        {
            int version = ++_templeAwakenRenderVersion;
            if (_box_temple_awaken != null) _box_temple_awaken.gameObject.SetActive(false);
            _ = RefreshTempleAwakenAsync(version);
        }

        private void OnRoleInfoUpdateForTempleAwaken()
        {
            int level = RoleModel.Instance.Level;
            if (level == _templeAwakenRoleLevel) return;
            _templeAwakenRoleLevel = level;
            RefreshTempleAwaken();
        }

        private async Task RefreshTempleAwakenAsync(int version)
        {
            await TempleAwakenConfigs.EnsureLoaded();
            if (this == null || _box_temple_awaken == null || version != _templeAwakenRenderVersion) return;

            TempleAwakenConfigs.HudInfo info = TempleAwakenConfigs.BuildHudInfo(
                TempleAwakenModel.Instance, RoleModel.Instance.Level, RoleModel.Instance.Career);
            if (info == null || !info.Visible) return;

            bool active = info.Active;
            _box_unopen_awaken.gameObject.SetActive(!active);
            _box_open_awaken.gameObject.SetActive(active);
            _img_awaken_red.gameObject.SetActive(active && info.CanReceive);
            _box_awaken_effect.gameObject.SetActive(false);

            if (!active)
            {
                _html_open_lv.text = info.Complete
                    ? "<color=#00FA64>已完成全部</color>"
                    : $"<color=#00FA64>{info.OpenLevel}</color>级解锁新任务";
                _box_temple_awaken.gameObject.SetActive(true);
                return;
            }

            _lb_open_awaken_progress.text = $"{info.CurrentStage}/{info.TotalStage}";
            _lb_open_awaken_task_desc.text = info.StageName ?? string.Empty;
            _lb_open_awaken_task_desc.color = info.CanReceive
                ? ParseColor("#00FA64")
                : Color.white;
            _img_goods_icon.enabled = false;
            _img_goods_name.enabled = false;

            Task<bool> iconTask = ResManager.SetImageAsync(
                _img_goods_icon, GameResPath.GetGoodsIconPath(info.GoodsId.ToString()), false, false);
            Task<bool> nameTask = ResManager.SetImageAsync(
                _img_goods_name, GameResPath.GetIcon("mainUI", "sdjx_" + info.ChapterId), false, false);
            bool[] loaded = await Task.WhenAll(iconTask, nameTask);
            if (this == null || _box_temple_awaken == null || version != _templeAwakenRenderVersion) return;

            _img_goods_icon.color = Color.white;
            _img_goods_name.color = Color.white;
            _img_goods_icon.enabled = loaded[0];
            _img_goods_name.enabled = loaded[1];
            _box_temple_awaken.gameObject.SetActive(true);
        }

        private void StartAutoTaskTimer()
        {
            CancelAutoTaskTimer();
            _autoTaskCts = new CancellationTokenSource();
            _ = AutoTaskLoop(_autoTaskCts.Token);
        }

        private void CancelAutoTaskTimer()
        {
            if (_autoTaskCts == null) return;
            _autoTaskCts.Cancel();
            _autoTaskCts.Dispose();
            _autoTaskCts = null;
        }

        private async Task AutoTaskLoop(CancellationToken token)
        {
            try
            {
                await Shenxiao.Framework.Util.TimeUtil.Delay(AutoTaskStartDelayMs, token); // ⚠ Task.Delay 在 WebGL 永不醒
                while (!token.IsCancellationRequested)
                {
                    await Shenxiao.Framework.Util.TimeUtil.Delay(AutoTaskPeriodMs, token);
                    TryAutoTask();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void TryAutoTask()
        {
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;
            if (RoleModel.Instance.Level >= AutoTaskMaxLevel) return;

            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (task == null) return;
            if (TaskModel.Instance.ShouldHoldAutoTaskForMainLineGuide(task)) return;

            if (DialogueModel.Instance.DialogIsOpen && task.Id > 0 && DialogueModel.Instance.CurrentNpcId == task.Id)
            {
                return;
            }

            // 防重入(对标老端 UpdateAutoFight 的 goToPos 闸):当前杀怪/收集任务已武装(weight=TASK 且
            // 锁定目标就是本任务的怪)时,10 秒兜底轮询不得重进 DoTask——否则走"已在附近→到达回调→重锁同一只怪"
            // 空转链每 10 秒刷屏一次(永动死循环的重入源)。目标怪死亡/离场时 GetClickTarget 自清返 null,
            // 条件不成立,轮询自动恢复"重新寻路/重新锁怪"的兜底职能。
            if ((task.TaskTipsType == TaskModel.TIP_KILL || task.TaskTipsType == TaskModel.TIP_ITEM)
                && AutoFight.AutoFightModel.Instance.AutoFightWeight == AutoFight.AutoFightModel.AUTO_WEIGHT_TASK
                && Scene.SceneCombat.Instance.GetClickTarget()?.TypeId == task.Id)
            {
                return;
            }

            TaskModel.Instance.FindNextAutoFightTask();
        }

        private static Color ParseColor(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
        }
    }
}
