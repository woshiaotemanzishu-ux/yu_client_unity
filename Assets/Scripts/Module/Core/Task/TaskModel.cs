using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
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
        public const int TIP_STREN_EQUIP = 8;    // 单件强化(id=+N,need=件数;→ EquipFlow 强化页)
        public const int TIP_FIN_DUN_TYPE = 9;   // 完成某类副本(id=DUN_TYPE;主线 100980 御魂本 type12 → DungeonRuneShellView)
        public const int TIP_JOIN_GUILD = 14;    // 加入结社(主线 101080 → GuildJoinShellView,40004 建社最短路径)
        public const int TIP_RUNE_NUM = 33;      // 灵魄镶嵌数(主线 100990 镶1次 → RuneWearShellView,16701)
        public const int TIP_DUNGEON_LEVEL = 57; // 副本关卡(id=DUN_TYPE,need=层;主线 101522 御魂本3层 → DungeonRuneShellView)
        public const int TIP_TRAIN_MOUNT = 23;   // 培养坐骑·阶星(id=阶,need=星;主线 100330「1阶2星」→ OutWardShellView)
        public const int TIP_STREN_SUM = 31;     // 全身强化总和(need=总等级;主线 100720「达8」→ EquipFlow 强化页)
        public const int TIP_OPEN_FUNCTION = 81; // 开启功能=天命觉醒专属(主线 100590 → TempleAwakenShellView,发 42900)
        public const int TIP_ACTIVE_SOAP = 89;   // 古宝激活(id=soap_id;主线 100811 幽瞳 → GuBaoShellView)
        public const int TIP_TRAIN_PARTNER = 25; // 培养同修(id=阶,need_num=星;主线首闸 100190「1阶2星」→ PartnerShellView)
        public const int TIP_AWARD_LV_GIFT = 54; // 领取等级礼包(id=礼包lv;主线 100420「领35级」→ RushGiftShellView)
        public const int TIP_SUIT_CLT = 84;      // 套装收集(id=suit_id,need=阶数;主线 100391「套装1到4」→ SuitCollectShellView)
        public const int TIP_MOUNT_LEVEL = 90;   // 外观等级线(id=type_id 1坐骑/2同修,need=等级;主线 100521/100901 → OutWardShellView)
        public const int TIP_LV = 27;         // 到达等级(老端开 UpAlertView 升级提醒)
        public const int TIP_WELCOME = 37;    // 进游戏欢迎(老端 case 为空 break,无动作)
        public const int TIP_COIN = 80;       // 上交铜钱

        // —— 主线链实际出现、老端 DoTask 走「开对应系统面板/引导」而该面板未移植的类型 → 结构化降级
        //(真实任务文案 toast + 精确 blocker 日志,不静默、不硬开空面板)。
        // 权威来源:yu_server data_task.erl get_content 全量统计(主线 type=1,543 任务/606 显式步骤),
        // 名称对照老端 TaskTipType 枚举(TaskModel.ts:52-243)。老端各 case 行为见 TaskModel.ts:797 switch。
        private static readonly Dictionary<int, string> UNPORTED_TIP_SYSTEM = new Dictionary<int, string>
        {
            // 8 StrenEquip 已接真实入口(第 18 轮:DoTask → EquipFlow.OpenSub 强化页,协议 15204/15205)
            // 9 FinDunType 已接真实入口(第 19 轮:DoTask → DungeonRuneShellView,协议 61001/61003/61020)
            { 11, "副本入口(具体副本id)" },        // FinDungeon(主线未出现,留降级)
            // 14 JoinGuild 已接真实入口(第 19 轮:DoTask → GuildJoinShellView,协议 40001/40003/40004)
            { 18, "装备熔炼" },                    // FusionEquip ×1
            // 23 TrainMount 已接真实入口(第 17 轮:DoTask → OutWardShellView,协议 16002/16023)
            // 24 TrainWing 已接真实入口(第 20 轮:DoTask → OutWardShellView type3,协议 16005)
            // 25 TrainPartner 已接真实入口(第 15 轮:DoTask → PartnerShellView,协议 14202/14205)
            { TIP_LV, "升级提醒(UpAlertView)" },   // LV ×57,老端 Fire(OPEN_UPALERT_VIEW);升级后服务端自动完成
            // 31 StrenSum 已接真实入口(第 18 轮:同 8 → EquipFlow 强化页)
            // 33 RuneNum 已接真实入口(第 19 轮:DoTask → RuneWearShellView,协议 16700/16701)
            // 35 JoinJcc 已接壳入口(第 21 轮:DoTask → JjcShellView,28001/02/03;壳内警示服务端计数断链 mod_jjc_cast.erl:87)
            // 41 TrainHolyorgan 已接真实入口(第 20 轮:DoTask → OutWardShellView type5,协议 16005)
            { 48, "宝石强化" },                    // EquipStoneNum ×2
            // 50 RuneLvSum 已接真实入口(第 21 轮:DoTask → RuneWearShellView 强化按钮,协议 16702)
            // 54 Award_lv_gift 已接真实入口(第 17 轮:DoTask → RushGiftShellView,协议 41700/41701)
            // 57 Dungeon_level 已接真实入口(第 19 轮:同 9 → DungeonRuneShellView)
            { 63, "大妖挑战" },                    // Kill_boss_id ×3
            // 73 Red_equip_combine 已接真实入口(第 21 轮:DoTask → ComposeShellView,通用合成 15020 type=2)
            // 81 Open_function 已接真实入口(第 18 轮:DoTask → TempleAwakenShellView,协议 42900)
            // 84 Suit_clt 已接真实入口(第 17 轮:DoTask → SuitCollectShellView,协议 15256/15257)
            // 89 ActiveSoap 已接真实入口(第 18 轮:DoTask → GuBaoShellView,协议 13320/13321)
            // 90 MountLevel 已接真实入口(第 17 轮:DoTask → OutWardShellView,协议 16028/16029)
            // 91 AfkReceiveTimes 已接真实入口(第 20 轮:DoTask → OnHookShellView,协议 13216)
            // 92 TrainArtifact(古法符相)已接真实入口(第 20 轮:DoTask → OutWardShellView type4,协议 16005)
            { 93, "橙装穿戴" },                    // DressOrangeEquip ×2
        };

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

        // 采集任务:采集物未在视野时,等其经九宫格(12007/12012)下发后再发起采集(对标击杀任务的 WaitTaskMonster)。
        private int _pendingCollectTaskId;
        private int _pendingCollectMonsterTypeId;
        private int _collectRetryToken; // 采集非成功终止后的延时重试令牌(防叠加多个重试)

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
                && task.TaskTipsType != TIP_COLLECT
                && task.TaskTipsType != TIP_PASS_MAIN_DUNGEON) return false;

            if (task.TaskTipsType == TIP_PASS_MAIN_DUNGEON)
            {
                StartPassMainDungeon(task);
                return true;
            }

            // 采集任务进度未满(多次采集):接着采下一个采集物(对标老端 CollectBarView 完成后 FindNextOne)。
            if (task.TaskTipsType == TIP_COLLECT)
            {
                StartTaskCollect(task);
                return true;
            }

            if (TryStartTaskAutoFight(task)) return true;
            WaitTaskMonster(task);
            return false;
        }

        public void ClearData()
        {
            StopWaitingTaskMonster();
            StopWaitingCollectMonster();
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

            // 3) 欢迎类:老端 case Welcome 为空 break(无动作)。
            if (task.TaskTipsType == TIP_WELCOME)
            {
                GameLog.Info("Task", "DoTask: Welcome(37) 无动作(对标老端空 case)");
                return;
            }

            // 3.5) 已接真实入口的「开系统面板」类(对标老端各 case 开对应面板;壳=TEMP,数据全真):
            // ★活服实证纠正(2026-07-03):TIP_TRAIN_PARTNER(25「剑魄同修培养」)判定走 pt_160 OutWard 的
            // type_id=2(?MATE_ID),与 TIP_TRAIN_MOUNT(23,type_id=1)同族——服务端 lib_task.erl TRAIN_PARTNER
            // 读 status_mount(mount 模块)的 type_id=2 阶星,靠 16023 一键升星推进。此前误路由到 pt_142
            // 神巫/companion 系统(PartnerShellView)——那套永不触发 lib_task_api,任务永远无法完成。
            // pt_142(config_companion/14204/14205)是另一独立系统,与 100190 无关。
            if (task.TaskTipsType == TIP_TRAIN_MOUNT || task.TaskTipsType == TIP_TRAIN_PARTNER
                || task.TaskTipsType == TIP_MOUNT_LEVEL
                || task.TaskTipsType == 24 || task.TaskTipsType == 92 || task.TaskTipsType == 41)
            {
                OutWard.OutWardShellView.Show(); return;   // 坐骑(23)/同修(25)阶星·等级(90) + 翼影24/圣器92/神兵41(16005 通用升星)
            }
            if (task.TaskTipsType == 91) { OnHook.OnHookShellView.Show(); return; }                               // 101211 挂机收益(13216)
            if (task.TaskTipsType == 50) { Rune.RuneWearShellView.Show(); return; }                               // 101525 御魂强化(16702)
            if (task.TaskTipsType == 73) { Compose.ComposeShellView.Show(); return; }                             // 101725 神装合成(15020)
            if (task.TaskTipsType == 35) { Jjc.JjcShellView.Show(); return; }                                     // 101465 排位(28001-03;壳警示服务端断链)
            if (task.TaskTipsType == TIP_SUIT_CLT) { SuitCollect.SuitCollectShellView.Show(); return; }           // 100391 套装收集(15256/57)
            if (task.TaskTipsType == TIP_AWARD_LV_GIFT) { RushGift.RushGiftShellView.Show(); return; }            // 100420 冲级豪礼(41700/01)
            if (task.TaskTipsType == TIP_STREN_EQUIP || task.TaskTipsType == TIP_STREN_SUM)
            {
                Equip.EquipFlow.OpenSub("EquipStrenView"); return;                                                // 100720 强化(15204/05;对标老端开锻造强化页)
            }
            if (task.TaskTipsType == TIP_OPEN_FUNCTION) { TempleAwaken.TempleAwakenShellView.Show(); return; }    // 100590 觉醒之路(42900)
            if (task.TaskTipsType == TIP_ACTIVE_SOAP) { GuBao.GuBaoShellView.Show(); return; }                    // 100811 古宝幽瞳(13320/21)
            if (task.TaskTipsType == TIP_FIN_DUN_TYPE || task.TaskTipsType == TIP_DUNGEON_LEVEL)
            {
                Dungeon.DungeonRuneShellView.Show(); return;                                                      // 100980/101522 御魂本(61001/03/20)
            }
            if (task.TaskTipsType == TIP_JOIN_GUILD) { Guild.GuildJoinShellView.Show(); return; }                 // 101080 结社(40001/03/04)
            if (task.TaskTipsType == TIP_RUNE_NUM) { Rune.RuneWearShellView.Show(); return; }                     // 100990 灵魄镶嵌(16700/01)

            // 4) 主线出现的「开系统面板」类(面板未移植)→ 结构化降级,先于通用寻路
            //(老端这些 case 不走坐标寻路:LV 开升级提醒、FinDunType 开副本入口等)。
            if (UNPORTED_TIP_SYSTEM.ContainsKey(task.TaskTipsType)) { DoDegradeTask(task); return; }

            // 5) 带场景坐标 → 寻路/切场景(对标 Kill/Collect/Item 等 case 的 pathfind / USE_FLY_SHOE)。
            if (task.SceneId > 0 && (task.SceneX > 0 || task.SceneY > 0)) { DoGotoSceneTask(task); return; }

            GameLog.Warn("Task", "DoTask blocker: tipsType={0} 未知类型(不在主线统计清单;对标 TaskModel.ts:797 switch 查行为后补)", task.TaskTipsType);
        }

        /// <summary>
        /// 「开系统面板」类任务的结构化降级:目标面板/系统未移植 → 真实任务文案(config tips/name)+ 系统名 toast,
        /// 精确 blocker 日志。玩家不落静默死路;等对应系统移植后逐个替换为真实入口(老端行为见 TaskModel.ts:797 对应 case)。
        /// 注:此类任务多为「达成条件服务端自动完成」(升级/培养/穿戴),点击仅是引导入口,降级不阻断主线数据推进。
        /// </summary>
        private void DoDegradeTask(TaskVo task)
        {
            string sys = UNPORTED_TIP_SYSTEM[task.TaskTipsType];
            string goal = string.IsNullOrEmpty(task.Tips) ? task.TaskName : task.Tips;
            TipsManager.Toast(goal + "(" + sys + " 未移植)");
            GameLog.Warn("Task", "DoTask degrade: id={0} tipsType={1} → {2} 未移植;老端行为=开对应面板/引导(TaskModel.ts:797)",
                task.TaskId, task.TaskTipsType, sys);
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
                GameLog.Info("Task",
                    "DoTask 找 NPC {0}: 在其他场景(任务场景={1},当前={2})→ 飞鞋跨场景,到达后续接对话",
                    task.Id, task.SceneId, curScene);
                RequestCrossSceneThenRetry(task);
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
                GameLog.Info("Task",
                    "DoTask 切场景: 任务 {0} 目标在场景 {1}(当前 {2}) → 飞鞋跨场景,到达后续接寻路。目标坐标 ({3},{4})。",
                    task.TaskId, task.SceneId, curScene, task.SceneX, task.SceneY);
                RequestCrossSceneThenRetry(task);
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

        /// <summary>正在等待跨场景落地后续接的任务(对标老端 fly_data.callback);null=无。</summary>
        private TaskVo _pendingCrossSceneTask;

        /// <summary>上次发跨场景请求的时间戳(realtimeSinceStartup),用于冷却防刷。</summary>
        private float _lastCrossSceneRequestAt;

        /// <summary>跨场景请求冷却(秒):同窗口内不重复发,防用户狂点 → 失败 12005 flood → 服务端 force-close。</summary>
        private const float CROSS_SCENE_COOLDOWN_SEC = 3f;

        /// <summary>
        /// 跨场景任务:发飞鞋切场景请求(<see cref="SceneController.RequestChangeScene"/>),落地后(新场景快照就绪
        /// EVT_SCENE_SNAPSHOT_READY)重跑 <see cref="DoTask"/> —— 此时 NPC/坐标已在当前场景,DoTask 自动走同场景
        /// 分支(走近 NPC/寻路)。对标老端 USE_FLY_SHOE → REQUEST_CHANGE_SCENE → 新场景 onSceneStart 续接回调。
        /// 仅重试一次,且续接前校验确实到达目标场景(防失败/错场景时死循环)。
        ///
        /// 两道门禁(防我此前引入的 flood 回归:玩家卡副本里狂点 → 失败 12005 把服务端刷到 force-close):
        ///   ① 副本内(DunId!=0)禁飞鞋(对标老端 IsCanUseFlyShoeScene 仅主城/野外安全区可飞;服务端会回
        ///      errorCode=1200001 拒,客户端先拦不发);② 冷却 CROSS_SCENE_COOLDOWN_SEC 内不重复发。
        /// </summary>
        private void RequestCrossSceneThenRetry(TaskVo task)
        {
            if (task == null || task.SceneId <= 0) return;

            if (RoleModel.Instance.DunId != 0)
            {
                GameLog.Warn("Task",
                    "跨场景任务 {0}: 当前在副本(dunId={1})内,不能飞鞋切场景(服务端会拒 errorCode=1200001)→ 需先退出副本。",
                    task.TaskId, RoleModel.Instance.DunId);
                return;
            }

            float now = UnityEngine.Time.realtimeSinceStartup;
            if (now - _lastCrossSceneRequestAt < CROSS_SCENE_COOLDOWN_SEC)
            {
                GameLog.Info("Task", "跨场景任务 {0}: 切场景冷却中(<{1:0.#}s),忽略重复触发(防刷)。", task.TaskId, CROSS_SCENE_COOLDOWN_SEC);
                return;
            }
            _lastCrossSceneRequestAt = now;

            _pendingCrossSceneTask = task;
            EventDispatcher.Off(GlobalEvent.EVT_SCENE_SNAPSHOT_READY, OnCrossSceneArrived);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_SNAPSHOT_READY, OnCrossSceneArrived);
            GameLog.Info("Task", "跨场景任务 {0}: 请求切到场景 {1} 落点({2},{3}),到达后续接任务",
                task.TaskId, task.SceneId, task.SceneX, task.SceneY);
            SceneController.Instance.RequestChangeScene(task.SceneId, task.SceneX, task.SceneY);
        }

        private void OnCrossSceneArrived()
        {
            TaskVo task = _pendingCrossSceneTask;
            _pendingCrossSceneTask = null;
            EventDispatcher.Off(GlobalEvent.EVT_SCENE_SNAPSHOT_READY, OnCrossSceneArrived);
            if (task == null) return;

            int curScene = RoleModel.Instance.SceneId;
            if (curScene != task.SceneId)
            {
                // 落地场景与目标不符(切场景失败/收到的是别的快照)→ 不续接,避免反复飞鞋死循环。
                GameLog.Warn("Task", "跨场景任务 {0}: 落地场景={1} 与目标={2} 不符,不续接", task.TaskId, curScene, task.SceneId);
                return;
            }
            GameLog.Info("Task", "跨场景任务 {0}: 已到达场景 {1},续接任务流(走近/寻路)", task.TaskId, curScene);
            DoTask(task);
        }

        private void DoPassMainDungeonTask(TaskVo task)
        {
            if (task == null) return;

            if (task.SceneId > 0 && (task.SceneX > 0 || task.SceneY > 0))
            {
                int curScene = RoleModel.Instance.SceneId;
                if (task.SceneId != 0 && task.SceneId != curScene)
                {
                    GameLog.Info("Task",
                        "PassMainDungeon 跨场景: task={0} target scene={1} current={2} → 飞鞋跨场景,到达后续接",
                        task.TaskId, task.SceneId, curScene);
                    RequestCrossSceneThenRetry(task);
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
                // ★活服实证修复(100170 卡点):进副本前武装自动战斗——老端靠「全局自动挂机常开」的隐含状态
                // 驱动副本内输出;Unity 自动化链此前没人置 AutoFightState → 副本内 20001 从未发出 →
                // 20s wave_timeout 判负(13306 state=1)无限重试。对标老端语义:任务驱动进副本=有仗要打。
                AutoFight.AutoFightModel.Instance.SetAutoFightWeight(AutoFight.AutoFightModel.AUTO_WEIGHT_TASK);
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
                StartTaskCollect(task);
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

        // ===================== 采集任务(TIP_COLLECT)=====================

        /// <summary>
        /// 采集任务:到任务点后找最近采集物(type=task.Id)→ 交 <see cref="CollectController.CollectMonster"/> 接近并采集
        /// (对标老端 Collect case → MainRoleAttackMonster 采集物分支 → OPEN_COLLECT_VIEW → RequestToCollect)。
        /// 采集物未在视野则挂 MonsterAdded 等其下发(对标击杀任务 WaitTaskMonster)。采集中则不重复驱动。
        /// </summary>
        private void StartTaskCollect(TaskVo task)
        {
            if (task == null) return;
            if (CollectController.Instance.IsCollecting)
            {
                GameLog.Info("Task", "collect 已在进行,跳过重复驱动 task={0}", task.TaskId);
                return;
            }
            if (task.Id <= 0)
            {
                GameLog.Warn("Task", "collect 任务 {0} 无采集物 type(task.Id=0),无法定位采集物", task.TaskId);
                return;
            }

            // 以主角当前位置为中心找最近采集物(对标老端 GetNearestMonsterByTypeId 取离玩家最近;重试时玩家可能已移动)。
            MonsterVo mon = SceneCombat.Instance.FindNearestCollectMonster(task.Id, RoleModel.Instance.X, RoleModel.Instance.Y);
            if (mon == null)
            {
                GameLog.Info("Task", "collect 任务 {0}: 采集物 type={1} 暂未在视野,等九宫格下发(monsterCount={2})",
                    task.TaskId, task.Id, SceneManager.Instance.MonsterCount);
                WaitCollectMonster(task);
                return;
            }

            StopWaitingCollectMonster();
            GameLog.Info("Task", "collect 任务 {0}: 采集物 ins={1} type={2} pos=({3},{4}) → 接近并采集",
                task.TaskId, mon.InstanceId, mon.TypeId, mon.X, mon.Y);
            CollectController.Instance.CollectMonster(mon);
        }

        private void WaitCollectMonster(TaskVo task)
        {
            if (task == null || task.Id <= 0) return;
            if (_pendingCollectTaskId == task.TaskId && _pendingCollectMonsterTypeId == task.Id) return;

            StopWaitingCollectMonster();
            _pendingCollectTaskId = task.TaskId;
            _pendingCollectMonsterTypeId = task.Id;
            SceneManager.Instance.MonsterAdded += OnCollectMonsterAdded;
            GameLog.Info("Task", "collect 任务 {0}: 挂 MonsterAdded 等采集物 type={1} 下发", task.TaskId, task.Id);
        }

        private void OnCollectMonsterAdded(MonsterVo vo)
        {
            if (_pendingCollectTaskId == 0 || vo == null) return;
            if (!vo.IsCollect) return;
            if (_pendingCollectMonsterTypeId > 0 && vo.TypeId != _pendingCollectMonsterTypeId) return;

            TaskVo task = MainLineTaskVo;
            if (task == null || task.TaskId != _pendingCollectTaskId || task.Id != _pendingCollectMonsterTypeId)
            {
                StopWaitingCollectMonster();
                return;
            }

            StopWaitingCollectMonster();
            StartTaskCollect(task);
        }

        private void StopWaitingCollectMonster()
        {
            if (_pendingCollectTaskId == 0) return;
            SceneManager.Instance.MonsterAdded -= OnCollectMonsterAdded;
            _pendingCollectTaskId = 0;
            _pendingCollectMonsterTypeId = 0;
        }

        /// <summary>
        /// 一次采集非成功终止(失败/取消/采集物被移除/START 超时,经 EVT_COLLECT_ENDED)→ 延时重试当前采集任务
        /// (对标老端 CollectBarView 失败后 1s FindNextOne)。采集成功走服务端 30001 推进,不经此路径(避免双重驱动)。
        /// 慢轮询(1.5s)而非紧打循环:争用(他人采集)/暂时不可采时定期重试,等可采或任务推进后自然收敛。
        /// </summary>
        public void OnCollectEnded()
        {
            if (!AutoTaskEnabled) return;
            TaskVo task = MainLineTaskVo;
            if (task == null || task.TaskTipsType != TIP_COLLECT || IsAllStepFinish(task.TaskId)) return;
            int token = ++_collectRetryToken;
            _ = RetryCollectAfterDelayAsync(task.TaskId, token);
        }

        private async Task RetryCollectAfterDelayAsync(int taskId, int token)
        {
            await Task.Delay(1500);
            if (token != _collectRetryToken || !AutoTaskEnabled) return;
            TaskVo task = MainLineTaskVo;
            if (task == null || task.TaskId != taskId || task.TaskTipsType != TIP_COLLECT || IsAllStepFinish(taskId)) return;
            if (CollectController.Instance.IsCollecting) return; // 期间已重新开始采集
            GameLog.Info("Task", "collect 任务 {0} 上次采集未成功 → 重试", taskId);
            StartTaskCollect(task);
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
