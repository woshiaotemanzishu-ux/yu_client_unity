using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoBrush;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Scene.Vo;

namespace Shenxiao.Module.Core.Tasks
{
    public sealed class TaskModel
    {
        public const int MAIN_LINE = 1;
        public const int AWAKE_LINE = 2;
        public const int REINCARNATION = 3;
        public const int EXTENSION_LINE = 5;
        public const int GUILD = 6;
        public const int DAILY = 7;
        public const int NORMAL_DAILY = 8;
        public const int EUDAEMON_TASK = 9;
        public const int KFHOLYAREA_TASK = 10;
        public const int GUIDE_TASK = 101870;
        // —— task_tips_type(服务端 pt_300 下发的提示类型;对标老端 TaskTipType,yu_client TaskModel.ts:52-243)——
        public const int TIP_KILL = 1;
        public const int TIP_ITEM = 3;
        public const int TIP_COLLECT = 4;
        public const int TIP_TALK = 5;        // 与 NPC 对话(可选)
        public const int TIP_START_TALK = 6;  // 开始对话(不可选)
        public const int TIP_END_TALK = 7;    // 结束对话(不可选)
        public const int TIP_PASS_MAIN_DUNGEON = 10;
        public const int TIP_COIN = 80;       // 上交铜钱

        // 自动寻路到任务点的到达半径(逻辑格;复用老端接近 NPC 的 dist=2.5,到点后怪已在九宫格视野内)。
        private const float TaskPointArriveLogicDist = 2.5f;
        private const float TaskJumpNormalMoveDistance = 1000f; // FLY_SHOE_DISTANCE.NormalMoveDistance
        private const float TaskJumpTwoDistance = 1600f;        // FLY_SHOE_DISTANCE.TwoJumoDistance
        private const float TaskJumpThreeDistance = 1800f;      // FLY_SHOE_DISTANCE.ThreeJumpDistance
        private const int TaskJumpTwoType = 4;
        private const int TaskJumpThreeType = 5;

        public static readonly TaskModel Instance = new TaskModel();

        private readonly Dictionary<int, List<TaskVo>> _hasReceiveTaskList = new Dictionary<int, List<TaskVo>>();
        private readonly Dictionary<int, List<TaskVo>> _canTaskList = new Dictionary<int, List<TaskVo>>();
        private readonly Dictionary<int, List<TaskVo>> _allTaskList = new Dictionary<int, List<TaskVo>>();

        private TaskFinishView _finishView; // 完成弹层(懒建复用,对标老端 TaskFinishView)

        private int _pendingAutoFightTaskId;
        private int _pendingAutoFightMonsterTypeId;

        private TaskModel() { }

        public int NowSelectTaskId { get; set; }
        public int NewestFinishTaskId { get; private set; }
        public TaskVo MainLineTaskVo { get; private set; }
        public bool AutoTaskEnabled { get; private set; } = true;

        public IReadOnlyDictionary<int, List<TaskVo>> AllTaskList => _allTaskList;

        /// <summary>该任务当前是否"可接"(在 can 列表)。对话空文本自动接受分支用(对标 GetCanTaskList)。</summary>
        public bool IsCanTask(int taskId) => _canTaskList.ContainsKey(taskId);

        /// <summary>该任务当前是否"已接"(在 received 列表)。对话空文本自动完成分支用(对标 GetHasReceiveTaskList)。</summary>
        public bool IsReceivedTask(int taskId) => _hasReceiveTaskList.ContainsKey(taskId);

        public bool GetAutoTaskSetting() => AutoTaskEnabled;

        public void SetAutoTaskSetting(bool enabled)
        {
            AutoTaskEnabled = enabled;
        }

        public void FindNextAutoFightTask()
        {
            if (!AutoTaskEnabled) return;

            TaskVo task = MainLineTaskVo;
            if (task == null)
            {
                GameLog.Info("Task", "FindNextAutoFightTask: no main-line task");
                return;
            }

            if (!ShouldAutoContinueTask(task))
            {
                GameLog.Info("Task", "FindNextAutoFightTask blocked: task={0} tipsType={1} finish={2}",
                    task.TaskId, task.TaskTipsType, task.HasFinish);
                return;
            }

            GameLog.Info("Task", "FindNextAutoFightTask: task={0} tipsType={1}", task.TaskId, task.TaskTipsType);
            DoTask(task);
        }

        public bool ResumeCurrentTaskAutoFight()
        {
            TaskVo task = MainLineTaskVo;
            if (task == null) return false;
            if (IsAllStepFinish(task.TaskId)) return false;
            if (task.TaskTipsType != TIP_KILL
                && task.TaskTipsType != TIP_ITEM
                && task.TaskTipsType != TIP_PASS_MAIN_DUNGEON) return false;

            if (task.TaskTipsType == TIP_PASS_MAIN_DUNGEON)
            {
                StartPassMainDungeon(task);
                return true;
            }

            if (TryStartTaskAutoFight(task)) return true;
            WaitTaskMonster(task);
            return false;
        }

        public void ClearData()
        {
            StopWaitingTaskMonster();
            _hasReceiveTaskList.Clear();
            _canTaskList.Clear();
            _allTaskList.Clear();
            MainLineTaskVo = null;
            NowSelectTaskId = 0;
        }

        public void SetNewestFinishTaskId(int taskId)
        {
            NewestFinishTaskId = taskId;
        }

        public void SetTaskLists(Dictionary<int, List<TaskVo>> canTaskList,
            Dictionary<int, List<TaskVo>> hasReceiveTaskList,
            Dictionary<int, List<TaskVo>> allTaskList)
        {
            Replace(_canTaskList, canTaskList);
            Replace(_hasReceiveTaskList, hasReceiveTaskList);
            Replace(_allTaskList, allTaskList);
            RefreshMainLineTaskVo();
        }

        public void UpdateTask(int taskId, List<TaskVo> tips)
        {
            if (taskId <= 0 || tips == null) return;
            _hasReceiveTaskList[taskId] = tips;
            _allTaskList[taskId] = tips;
            RefreshMainLineTaskVo();
        }

        public List<TaskEntry> GetTaskListForMainUI()
        {
            List<TaskEntry> result = new List<TaskEntry>();
            foreach (KeyValuePair<int, List<TaskVo>> kv in _allTaskList)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                if (IsAwakeTask(kv.Key) || IsEudaemonTask(kv.Key) || IsKKHolyAreaTask(kv.Key)) continue;
                if (IsMainTask(kv.Key) && MainLineTaskNeedShowArrow()) continue;

                (int sortIndex, int sortSubIndex, int sameTypeOrderIndex) = GetSortIndex(kv.Key);
                result.Add(new TaskEntry
                {
                    TaskId = kv.Key,
                    SortIndex = sortIndex,
                    SortSubIndex = sortSubIndex,
                    SameTypeOrderIndex = sameTypeOrderIndex,
                    TipsList = kv.Value,
                });
            }

            if (result.Count == 1 && MainLineTaskVo != null && !MainLineTaskNeedShowArrow())
            {
                result.Clear();
            }

            result.Sort(CompareTaskEntry);
            return result;
        }

        /// <summary>
        /// 主线引导任务条目(对标老端 SetMainLineTask:当 MainLineTaskNeedShowArrow 时,主线任务从
        /// 面板列表里排除,改放 _box_main_line 单独渲染)。返回主线任务的 TaskEntry(含 tips 列表),无则 null。
        /// </summary>
        public TaskEntry GetMainLineEntry()
        {
            if (MainLineTaskVo == null) return null;
            int id = MainLineTaskVo.TaskId;
            if (!_allTaskList.TryGetValue(id, out List<TaskVo> tips) || tips == null || tips.Count == 0) return null;
            (int sortIndex, int sortSubIndex, int sameTypeOrderIndex) = GetSortIndex(id);
            return new TaskEntry
            {
                TaskId = id,
                SortIndex = sortIndex,
                SortSubIndex = sortSubIndex,
                SameTypeOrderIndex = sameTypeOrderIndex,
                TipsList = tips,
            };
        }

        public TaskVo FindUnFinishTask(List<TaskVo> tipsList)
        {
            if (tipsList == null || tipsList.Count == 0) return null;
            TaskVo task = tipsList[0];
            for (int i = 0; i < tipsList.Count; i++)
            {
                if (tipsList[i].HasFinish != 1)
                {
                    task = tipsList[i];
                    break;
                }
            }
            return task;
        }

        public bool IsAllStepFinish(int taskId)
        {
            if (!_allTaskList.TryGetValue(taskId, out List<TaskVo> tips) || tips == null || tips.Count == 0) return false;
            for (int i = 0; i < tips.Count; i++)
            {
                if (tips[i].HasFinish != 1) return false;
            }
            return true;
        }

        public string GetTaskTagName(int taskType)
        {
            if (taskType == MAIN_LINE) return "主";
            if (taskType == DAILY || taskType == NORMAL_DAILY || taskType == EUDAEMON_TASK) return "日";
            if (taskType == GUILD) return "结";
            if (taskType == REINCARNATION) return "转";
            if (taskType == AWAKE_LINE) return "唤";
            return "支";
        }

        public string GetTaskColor(int taskType)
        {
            if (taskType == MAIN_LINE) return "#ff9015";
            if (taskType == REINCARNATION) return "#a376ff";
            return "#60aeff";
        }

        public string BuildMainUITips(TaskVo task)
        {
            if (task == null) return "";

            string tips = BuildTipBody(task);
            bool finish = IsAllStepFinish(task.TaskId);
            if (finish)
            {
                if (!IsFindNpcTask(task.TaskTipsType)) tips += "(完成)";
            }
            else if (task.ShowNum > 0)
            {
                tips += " (0/" + task.ShowNum + ")";
            }
            else
            {
                tips += " (" + task.NowNum + "/" + task.NeedNum + ")";
            }
            return tips;
        }

        /// <summary>
        /// 任务条文案主体(对标老端 GetTaskTipsMsgByMainUITaskItem,TaskModel.ts:2530)。老端文案是
        /// **客户端按类型现拼**,不是直接用 config_task.tips:找 NPC 对话类 = "与<NPC名>交谈"(NPC 名取
        /// config_npc,绿色)。本轮先补主线首屏最常见的对话类(Talk/StartTalk/EndTalk);击杀/采集/收集/
        /// 通关副本等类型仍回退服务端 tipsMsg / config tips,后续轮按 config_mon/config_goods 逐类补。
        /// </summary>
        private string BuildTipBody(TaskVo task)
        {
            if (IsFindNpcTask(task.TaskTipsType) && task.Id > 0)
            {
                NpcConfigs.NpcCfg npc = NpcConfigs.Get(task.Id);
                if (npc != null && !string.IsNullOrEmpty(npc.Name))
                    return "与<color=#00fa64>" + npc.Name + "</color>交谈";
            }
            return string.IsNullOrEmpty(task.TaskTipsMsg) ? task.Tips : task.TaskTipsMsg;
        }

        public bool IsMainTask(int taskId)
        {
            return TaskConfigs.Get(taskId)?.Type == MAIN_LINE;
        }

        public bool IsAwakeTask(int taskId)
        {
            return TaskConfigs.Get(taskId)?.Type == AWAKE_LINE;
        }

        public bool IsEudaemonTask(int taskId)
        {
            return TaskConfigs.Get(taskId)?.Type == EUDAEMON_TASK;
        }

        public bool IsKKHolyAreaTask(int taskId)
        {
            return TaskConfigs.Get(taskId)?.Type == KFHOLYAREA_TASK;
        }

        public bool MainLineTaskNeedShowArrow()
        {
            return MainLineTaskVo != null
                && MainLineTaskVo.TaskType == MAIN_LINE
                && MainLineTaskVo.TaskId <= GUIDE_TASK;
        }

        public TaskGuideStep GetNowGuideCfg(bool isMainUiArrow, TaskVo task = null)
        {
            task ??= MainLineTaskVo;
            if (task == null) return null;

            return TaskGuideConfigs.GetNowGuideCfg(isMainUiArrow, task);
        }

        public bool ShouldHoldAutoTaskForMainLineGuide(TaskVo task = null)
        {
            task ??= MainLineTaskVo;
            return task != null
                && task.TaskType == MAIN_LINE
                && task.TaskId <= GUIDE_TASK
                && TaskGuideConfigs.HasVisibleMainUiTaskGuide(task);
        }

        public (int sortIndex, int sortSubIndex, int sameTypeOrderIndex) GetSortIndex(int taskId)
        {
            TaskConfigs.TaskCfg cfg = TaskConfigs.Get(taskId);
            if (cfg == null) return (99, 99, 0);

            bool finish = IsAllStepFinish(taskId);
            int index;
            int subIndex;
            if (finish)
            {
                index = 3;
                subIndex = GetTypeSortOrder(cfg.Type);
            }
            else
            {
                index = GetTypeSortOrder(cfg.Type);
                subIndex = index;
            }
            return (index == 0 ? 99 : index, subIndex == 0 ? 99 : subIndex, cfg.Sep);
        }

        private void RefreshMainLineTaskVo()
        {
            MainLineTaskVo = null;
            foreach (KeyValuePair<int, List<TaskVo>> kv in _allTaskList)
            {
                if (!IsMainTask(kv.Key)) continue;
                MainLineTaskVo = FindUnFinishTask(kv.Value);
                StopWaitingIfMainTaskChanged();
                return;
            }
            StopWaitingIfMainTaskChanged();
        }

        private static int GetTypeSortOrder(int taskType)
        {
            if (taskType == MAIN_LINE) return 2;
            if (taskType == REINCARNATION) return 4;
            if (taskType == DAILY) return 5;
            if (taskType == GUILD) return 6;
            if (taskType == EXTENSION_LINE) return 7;
            if (taskType == NORMAL_DAILY) return 8;
            if (taskType == EUDAEMON_TASK) return 9;
            return 99;
        }

        private static bool IsFindNpcTask(int taskTipsType)
        {
            // 对标 TaskModel.ts:2966-2972 IsFindNpcTask:Talk/StartTalk/EndTalk 三类为"找 NPC 对话"任务。
            return taskTipsType == TIP_TALK || taskTipsType == TIP_START_TALK || taskTipsType == TIP_END_TALK;
        }

        private bool ShouldAutoContinueTask(TaskVo task)
        {
            if (task == null) return false;
            if (IsAllStepFinish(task.TaskId)) return true;
            if (IsFindNpcTask(task.TaskTipsType)) return true;
            return task.TaskTipsType == TIP_KILL
                || task.TaskTipsType == TIP_ITEM
                || task.TaskTipsType == TIP_COLLECT
                || task.TaskTipsType == TIP_PASS_MAIN_DUNGEON;
        }

        /// <summary>
        /// 任务点击主入口(最小等价,对标老端 TaskModel.DoTask,yu_client TaskModel.ts:744-784 + 797 switch)。
        /// 先置选中态(NowSelectTaskId)并广播 EVT_TASK_SELECT_CHANGED(对标老端 CLICK_DO_TASK),再按
        /// task_tips_type 进入正确分支:找 NPC 对话(Talk/StartTalk/EndTalk)→ 定位 NPC;完成且非对话 → 完成弹层;
        /// 带场景坐标 → 寻路/切场景。未移植的子系统(对话/完成弹层/寻路)给精确 blocker,不臆造、不假装完成。
        /// </summary>
        public void DoTask(TaskVo task)
        {
            if (task == null) task = MainLineTaskVo;
            if (task == null) { GameLog.Warn("Task", "DoTask: 无可执行任务(传入 null 且无主线任务)"); return; }

            // 选中态(对标 now_select_task_id):点任务即设选中并广播,任务栏据此刷新 _img_select。
            NowSelectTaskId = task.TaskId;
            EventDispatcher.Emit(GlobalEvent.EVT_TASK_SELECT_CHANGED, task.TaskId);
            GameLog.Info("Task", "DoTask: id={0} tipsType={1} finish={2} npcId={3} scene=({4},{5},{6})",
                task.TaskId, task.TaskTipsType, task.HasFinish, task.Id, task.SceneId, task.SceneX, task.SceneY);

            // 1) 找 NPC 对话任务(对标 ts:783 finish 早退排除 find-npc + ts:1767 Talk/StartTalk/EndTalk case)。
            if (IsFindNpcTask(task.TaskTipsType)) { DoFindNpcTask(task); return; }

            // 2) 全部完成且非对话 → 打开完成弹层真实入口(对标 ts:2385 TASK_OPEN_VIEW 'TaskFinishView')。
            if (IsAllStepFinish(task.TaskId)) { DoFinishTask(task); return; }

            if (task.TaskTipsType == TIP_PASS_MAIN_DUNGEON) { DoPassMainDungeonTask(task); return; }

            // 3) 带场景坐标 → 寻路/切场景(对标 Kill/Collect/Item 等 case 的 pathfind / USE_FLY_SHOE)。
            if (task.SceneId > 0 && (task.SceneX > 0 || task.SceneY > 0)) { DoGotoSceneTask(task); return; }

            GameLog.Warn("Task", "DoTask blocker: tipsType={0} 其余 case 未移植(对标 TaskModel.ts:797 switch 的 60+ case)。" +
                "当前最小入口只覆盖 对话/完成/场景坐标 三类;其余(开背包/锻造/进副本等)按需逐 case 补。", task.TaskTipsType);
        }

        /// <summary>
        /// 找 NPC 对话(对标 ts:1767-1835 Talk/StartTalk/EndTalk)。定位 NPC → 主角走到其身边 → 到达后打开
        /// 真实对话(12101/12102)。完整对标老端 Scene.MainRoleToNpc:走到 NPC(dist≤2.5 逻辑格)、停下转身后
        /// 才 Fire(SHOW_TASK);走近由 <see cref="MainRoleAgent.MoveToNpc"/> 直线接近(含卡死/超时兜底,无 A*)实现。
        /// 去重(对话已开不重复)由 DialogueController/DialogueModel.DialogIsOpen 负责。
        /// </summary>
        private void DoFindNpcTask(TaskVo task)
        {
            if (task.Id == 0) { GameLog.Info("Task", "DoTask: 自言自语任务(npcId=0),无目标 NPC,跳过"); return; }

            NpcVo npc = SceneManager.Instance.GetNpc(task.Id);
            if (npc == null)
            {
                int curScene = RoleModel.Instance.SceneId;
                if (task.SceneId == 0 || task.SceneId == curScene)
                {
                    // 目标 NPC 不是当前场景的可见对象:多为 config_npc scene:0 的对话型/浮空 NPC(如灵枢仙子 100134,
                    // 仅发新手武器对话,x/y=0 无固定坐标)。老端对这类 NPC 不走近,直接开对话(12101,不依赖场景对象)。
                    GameLog.Info("Task", "DoTask 找 NPC {0}:非场景可见对象(对话型 NPC),直接打开对话(12101)", task.Id);
                    DialogueController.Instance.ShowTask(task.Id);
                    return;
                }
                GameLog.Warn("Task",
                    "DoTask 找 NPC blocker: 目标 NPC {0} 在其他场景(任务场景={1},当前={2})→ 跨场景切换" +
                    "(老端 USE_FLY_SHOE/飞鞋协议)未移植 → blocker。", task.Id, task.SceneId, curScene);
                return;
            }

            int npcId = task.Id;
            MainRoleAgent agent = MainRoleAgent.Current;
            if (agent == null)
            {
                // 主角驱动尚未装配(进游戏后理论上恒在):不软锁,直接开对话。
                GameLog.Warn("Task", "DoTask 找 NPC: 无 MainRoleAgent,跳过走近,直接打开对话(12101)");
                DialogueController.Instance.ShowTask(npcId);
                return;
            }

            // 对标 Scene.MainRoleToNpc:主角走到 NPC 身边(dist≤2.5 逻辑格)、停下转身后才打开对话(发 12101)。
            GameLog.Info("Task", "DoTask 找 NPC: NPC {0} 在场景 pos=({1},{2}),主角走过去,到达后开对话(12101)", npcId, npc.X, npc.Y);
            if (agent.IsDirectPathBlockedTo(npc.X, npc.Y))
            {
                GameLog.Info("Task", "DoTask 找 NPC: 直线路径被阻挡,使用任务跳跃跨越地形 npc={0} target=({1},{2})",
                    npcId, npc.X, npc.Y);
                agent.TaskJumpTo(npc.X, npc.Y, TaskJumpTwoType, () => DialogueController.Instance.ShowTask(npcId));
                return;
            }

            agent.MoveToNpcStrict(npc.X, npc.Y, 0f, () => DialogueController.Instance.ShowTask(npcId));
        }

        /// <summary>完成提交(对标 ts:2385:TaskFinishView/TaskCircleFinishView + 协议 30004)。</summary>
        private void DoFinishTask(TaskVo task)
        {
            // 打开完成弹层:展示 config_task 真实奖励 + "领取奖励/提交任务"按钮(发 30004);
            // 对标老端 Fire(TASK_OPEN_VIEW, 'TaskFinishView')。不跳弹层直发 30004——老端要求经弹层确认/领奖再提交。
            if (_finishView == null) _finishView = new TaskFinishView();
            _finishView.Open(task);
            GameLog.Info("Task", "DoTask 完成: 任务 {0} 全步完成 → 打开 TaskFinishView(展示奖励 + 提交 30004)", task.TaskId);
        }

        /// <summary>
        /// 带场景坐标的任务(对标老端 Kill/Collect/Item case:同场景自动寻路到点,跨场景飞鞋)。
        /// 同场景:复用 <see cref="MainRoleAgent.MoveToNpc"/> 直线接近任务目标点(对标老端 DoTask 自动寻路 + TaskSpeed
        /// 把主角带到任务坐标)。寻路途中服务器按九宫格(12012/12007)把目标点附近的怪物下发到 SceneManager;
        /// 到点后由技能点击(SkillController → SceneCombat.MainRoleAttackTarget)命中真实怪(P3)。
        /// 跨场景:老端走 USE_FLY_SHOE/飞鞋切场景协议,本端未移植 → 记录真实 blocker(不臆造切场景)。
        /// </summary>
        private void DoGotoSceneTask(TaskVo task)
        {
            int curScene = RoleModel.Instance.SceneId;
            if (task.SceneId != 0 && task.SceneId != curScene)
            {
                GameLog.Warn("Task",
                    "DoTask 切场景 blocker: 任务 {0} 目标在场景 {1}(当前 {2}),跨场景切换(老端 USE_FLY_SHOE/飞鞋)" +
                    "未移植 → blocker。目标坐标 ({3},{4}) 已就绪。", task.TaskId, task.SceneId, curScene, task.SceneX, task.SceneY);
                return;
            }

            MainRoleAgent agent = MainRoleAgent.Current;
            if (agent == null)
            {
                GameLog.Warn("Task",
                    "DoTask 寻路 blocker: 任务 {0} 同场景目标 ({1},{2}),但主角驱动(MainRoleAgent)未装配 → 无法自动寻路。",
                    task.TaskId, task.SceneX, task.SceneY);
                return;
            }

            // 同场景自动寻路(直线接近 + 撞墙滑行 + 卡死/超时兜底,对标老端 DoTask 自动寻路/TaskSpeed)。
            GameLog.Info("Task",
                "DoTask 寻路: 任务 {0} 同场景自动寻路到目标点 ({1},{2})(对标老端 DoTask 自动寻路/TaskSpeed);" +
                "途中目标点附近怪物由九宫格(12012/12007)真实下发到 SceneManager,命中走技能点击(SceneCombat)。",
                task.TaskId, task.SceneX, task.SceneY);
            if (TryStartTaskJumpToPoint(agent, task)) return;
            agent.MoveToNpcStrict(task.SceneX, task.SceneY, TaskPointArriveLogicDist, () => OnArriveTaskPoint(task));
        }

        private void DoPassMainDungeonTask(TaskVo task)
        {
            if (task == null) return;

            if (task.SceneId > 0 && (task.SceneX > 0 || task.SceneY > 0))
            {
                int curScene = RoleModel.Instance.SceneId;
                if (task.SceneId != 0 && task.SceneId != curScene)
                {
                    GameLog.Warn("Task",
                        "PassMainDungeon blocker: task={0} target scene={1} current={2}; cross-scene fly/change path not migrated yet",
                        task.TaskId, task.SceneId, curScene);
                    return;
                }

                MainRoleAgent agent = MainRoleAgent.Current;
                if (agent == null)
                {
                    GameLog.Warn("Task", "PassMainDungeon blocker: task={0} has no MainRoleAgent", task.TaskId);
                    return;
                }

                GameLog.Info("Task", "PassMainDungeon move to entry: task={0} pos=({1},{2})",
                    task.TaskId, task.SceneX, task.SceneY);
                if (TryStartTaskJumpToPoint(agent, task)) return;
                agent.MoveToNpcStrict(task.SceneX, task.SceneY, TaskPointArriveLogicDist, () => StartPassMainDungeon(task));
                return;
            }

            StartPassMainDungeon(task);
        }

        private void StartPassMainDungeon(TaskVo task)
        {
            if (task == null) return;

            if (IsAutoBrushProgressReady())
            {
                GameLog.Info("Task", "PassMainDungeon request dungeon enter: task={0} proto=13305 type=0", task.TaskId);
                AutoBrushController.Instance.RequestEnterOrExit(0);
                return;
            }

            if (!AutoBrushModel.Instance.AutoBrushState)
            {
                GameLog.Info("Task", "PassMainDungeon request auto-brush open: task={0} proto=13307 type=0", task.TaskId);
                AutoBrushController.Instance.RequestAutoBrushState(true);
                return;
            }

            if (!TryStartTaskAutoFight(task))
            {
                WaitTaskMonster(task);
            }
        }

        private static bool IsAutoBrushProgressReady()
        {
            AutoBrushModel.BrushStrangeInfo info = AutoBrushModel.Instance.BrushInfo;
            return info != null && info.NeedTimes > 0 && info.CurrentTimes >= info.NeedTimes;
        }

        private bool TryStartTaskJumpToPoint(MainRoleAgent agent, TaskVo task)
        {
            if (agent == null || task == null) return false;

            bool blocked = agent.IsDirectPathBlockedTo(task.SceneX, task.SceneY);
            int jumpType = ResolveTaskJumpType(task.SceneX, task.SceneY);
            if (jumpType == 0 && blocked) jumpType = TaskJumpTwoType;
            if (jumpType == 0) return false;

            GameLog.Info("Task", "DoTask task jump: task={0} jumpType={1} blocked={2} target=({3},{4})",
                task.TaskId, jumpType, blocked, task.SceneX, task.SceneY);
            agent.TaskJumpTo(task.SceneX, task.SceneY, jumpType, () => OnArriveTaskPoint(task));
            return true;
        }

        private static int ResolveTaskJumpType(int targetX, int targetY)
        {
            RoleModel role = RoleModel.Instance;
            double dx = targetX - role.X;
            double dy = targetY - role.Y;
            double distance = System.Math.Sqrt(dx * dx + dy * dy);
            if (distance <= TaskJumpNormalMoveDistance) return 0;
            if (distance <= TaskJumpTwoDistance) return TaskJumpTwoType;
            if (distance <= TaskJumpThreeDistance) return TaskJumpThreeType;
            return 0;
        }

        /// <summary>寻路到达任务点(对标老端到点后停下;击杀类在此由技能点击命中真实怪,完整自动战斗循环=后续轮)。</summary>
        private void OnArriveTaskPoint(TaskVo task)
        {
            if (task == null) return;

            if (task.TaskTipsType == TIP_KILL
                || task.TaskTipsType == TIP_ITEM
                || task.TaskTipsType == TIP_PASS_MAIN_DUNGEON)
            {
                if (task.TaskTipsType == TIP_PASS_MAIN_DUNGEON && !AutoBrushModel.Instance.AutoBrushState)
                {
                    StartPassMainDungeon(task);
                }
                else if (!TryStartTaskAutoFight(task))
                {
                    WaitTaskMonster(task);
                }
                return;
            }

            if (task.TaskTipsType == TIP_COLLECT)
            {
                GameLog.Warn("Task", "auto task collect blocked: task={0} collect target={1}, collect protocol not migrated yet",
                    task.TaskId, task.Id);
                return;
            }

            GameLog.Info("Task",
                "DoTask 到达任务点 {0}({1},{2}):场景怪物数={3}(九宫格真实下发)。击杀类任务在此由技能点击" +
                "(SceneCombat.MainRoleAttackTarget)命中真实怪;无怪则按真实语义阻塞,不假放。",
                task.TaskId, task.SceneX, task.SceneY, SceneManager.Instance.MonsterCount);
        }

        private bool TryStartTaskAutoFight(TaskVo task)
        {
            if (task == null) return false;
            if (task.TaskTipsType != TIP_PASS_MAIN_DUNGEON && task.Id <= 0) return false;

            bool locked = task.Id > 0
                ? SceneCombat.Instance.TrySetNearestMonsterByType(task.Id, task.SceneX, task.SceneY)
                : SceneCombat.Instance.TrySetNearestAttackableMonster(task.SceneX, task.SceneY);
            if (!locked)
            {
                GameLog.Warn("Task", "auto task kill waiting: task={0} monsterType={1} no attackable monster yet, monsterCount={2}",
                    task.TaskId, task.Id, SceneManager.Instance.MonsterCount);
                return false;
            }

            StopWaitingTaskMonster();
            AutoFightModel.Instance.SetAutoFightWeight(AutoFightModel.AUTO_WEIGHT_TASK);
            GameLog.Info("Task", "auto task kill started: task={0} monsterType={1}", task.TaskId, task.Id);
            return true;
        }

        private void WaitTaskMonster(TaskVo task)
        {
            if (task == null) return;
            if (task.Id <= 0 && task.TaskTipsType != TIP_PASS_MAIN_DUNGEON) return;
            if (_pendingAutoFightTaskId == task.TaskId && _pendingAutoFightMonsterTypeId == task.Id) return;

            StopWaitingTaskMonster();
            _pendingAutoFightTaskId = task.TaskId;
            _pendingAutoFightMonsterTypeId = task.Id;
            SceneManager.Instance.MonsterAdded += OnTaskMonsterAdded;
            GameLog.Info("Task", "auto task kill wait MonsterAdded: task={0} monsterType={1} center=({2},{3})",
                task.TaskId, task.Id, task.SceneX, task.SceneY);
        }

        private void OnTaskMonsterAdded(MonsterVo vo)
        {
            if (_pendingAutoFightTaskId == 0 || vo == null) return;
            if (_pendingAutoFightMonsterTypeId > 0 && vo.TypeId != _pendingAutoFightMonsterTypeId) return;
            if (vo.IsCollect || vo.CanAttack != 1 || vo.Hp <= 0) return;

            TaskVo task = MainLineTaskVo;
            if (task == null || task.TaskId != _pendingAutoFightTaskId || task.Id != _pendingAutoFightMonsterTypeId)
            {
                StopWaitingTaskMonster();
                return;
            }

            TryStartTaskAutoFight(task);
        }

        private void StopWaitingTaskMonster()
        {
            if (_pendingAutoFightTaskId == 0) return;
            SceneManager.Instance.MonsterAdded -= OnTaskMonsterAdded;
            _pendingAutoFightTaskId = 0;
            _pendingAutoFightMonsterTypeId = 0;
        }

        private void StopWaitingIfMainTaskChanged()
        {
            if (_pendingAutoFightTaskId == 0) return;
            if (MainLineTaskVo == null
                || MainLineTaskVo.TaskId != _pendingAutoFightTaskId
                || MainLineTaskVo.Id != _pendingAutoFightMonsterTypeId
                || MainLineTaskVo.HasFinish == 1)
            {
                StopWaitingTaskMonster();
            }
        }

        private static int CompareTaskEntry(TaskEntry a, TaskEntry b)
        {
            int v = a.SortIndex.CompareTo(b.SortIndex);
            if (v != 0) return v;
            v = a.SortSubIndex.CompareTo(b.SortSubIndex);
            if (v != 0) return v;
            v = a.SameTypeOrderIndex.CompareTo(b.SameTypeOrderIndex);
            if (v != 0) return v;
            return a.TaskId.CompareTo(b.TaskId);
        }

        private static void Replace(Dictionary<int, List<TaskVo>> dst, Dictionary<int, List<TaskVo>> src)
        {
            dst.Clear();
            if (src == null) return;
            foreach (KeyValuePair<int, List<TaskVo>> kv in src)
            {
                dst[kv.Key] = kv.Value;
            }
        }

        public sealed class TaskEntry
        {
            public int TaskId;
            public int SortIndex;
            public int SortSubIndex;
            public int SameTypeOrderIndex;
            public List<TaskVo> TipsList;
        }

        public sealed class TaskGuideStep
        {
            public int Direction;
            public string Text;
            public int CloseTime;
            public bool AutoCountdown;
            public bool NotShowInTaskItem;
            public bool NotEffect;
            public float EffectScaleX = 1f;
            public float EffectScaleY = 1f;
            public float EffectScaleZ = 1f;
            public float FingerOffsetX;
            public float FingerOffsetY;
            public float OffsetX;
            public float OffsetY;
        }
    }
}
