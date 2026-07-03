using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Compose;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.GuBao;
using Shenxiao.Module.Core.Guild;
using Shenxiao.Module.Core.OnHook;
using Shenxiao.Module.Core.OutWard;
using Shenxiao.Module.Core.Partner;
using Shenxiao.Module.Core.Rune;
using Shenxiao.Module.Core.RushGift;
using Shenxiao.Module.Core.SuitCollect;
using Shenxiao.Module.Core.TempleAwaken;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// ★测试专用代行器,非老端玩家行为★——PlaySmoke 无人值守验收用。
    ///
    /// 主线上「系统类」任务(培养同修/坐骑升星/套装激活/领取礼包等)在真实老端里是玩家手点各系统面板的
    /// 按钮完成的;<see cref="TaskModel.FindNextAutoFightTask"/> 只接管「打怪/拾取/采集/过副本」这类
    /// 可无脑寻路的任务类型,系统类任务永远卡在原地等真人点击。本类是仅供 PlaySmoke 冒烟场景使用的
    /// "代玩家点击"垫片:双重门禁通过时,定时探测当前主线任务的 TaskTipsType,照抄一次对应系统面板
    /// 玩家会点的那个按钮的协议调用,把主线推进下去,好让无人值守流程能验收更深的主线段。
    ///
    /// 正式玩家路径永远是:玩家打开对应系统壳(PartnerShellView/OutWardShellView/...) → 手点培养/升星/
    /// 领取等按钮 → 各 Controller 发协议。本类不替代、不下线那条路径,只是在测试门禁开启时并行代跑。
    ///
    /// 门控(缺一不可):
    ///   ① 命令行含 -shenxiaoPlaySmoke(照 LoginBootstrap.GetCommandLineForcesSmoke 的参数读取模式);
    ///   ② TaskModel.Instance.GetAutoTaskSetting() 为 true(自动任务总开关)。
    /// </summary>
    public static class TaskSystemAutoPilot
    {
        private const int TICK_MS = 20_000;
        private const double ACTION_COOLDOWN_SEC = 60d;

        // GuBao 主线 100811 固定 soap=10001「幽瞳」的两片碎片(对标 GuBaoController 类注释)。
        private static readonly int[] GuBaoDebrisIds = { 1105010011, 1105010012 };

        // GoodsModel 物品大类:14=宝石(对标 ItemTipsView 展示分支注释)。
        private const int GOODS_TYPE_STONE = 14;

        private static CancellationTokenSource _loopCts;

        // 同任务同动作 60s 冷却防刷(key = taskId*1000 + tipsType,防止同 tick 反复重发同一动作打爆服务端)。
        private static readonly Dictionary<long, double> _lastActionAt = new Dictionary<long, double>();

        /// <summary>由 TaskController.Register 末尾调用。双门禁不过则不启动(静默,不打印噪声日志)。</summary>
        public static void Init()
        {
            if (_loopCts != null) return; // 已启动,可重入
            if (!IsCommandLineSmoke()) return;
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;

            _loopCts = new CancellationTokenSource();
            _ = RunLoopAsync(_loopCts);
            GameLog.Info("Task", "autopilot: started(-shenxiaoPlaySmoke + AutoTaskSetting,测试专用,非老端玩家行为)");
        }

        /// <summary>由 TaskController.Dispose 调用,断线/登出时收停。</summary>
        public static void Shutdown()
        {
            if (_loopCts == null) return;
            CancellationTokenSource cts = _loopCts;
            _loopCts = null;
            cts.Cancel();
            cts.Dispose();
            _lastActionAt.Clear();
        }

        /// <summary>照 LoginBootstrap.GetCommandLineForcesSmoke 的参数读取模式。</summary>
        private static bool IsCommandLineSmoke()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-shenxiaoPlaySmoke", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static async Task RunLoopAsync(CancellationTokenSource cts)
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        Tick();
                    }
                    catch (Exception e)
                    {
                        GameLog.Warn("Task", "autopilot tick exception: {0}", e.Message);
                    }
                    await Task.Delay(TICK_MS, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止(登出/断线 Shutdown)。
            }
            finally
            {
                if (_loopCts == cts) _loopCts = null;
            }
        }

        private static void Tick()
        {
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;

            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (task == null || TaskModel.Instance.IsAllStepFinish(task.TaskId)) return; // FindNextAutoFightTask 会接手

            if (!TryConsumeCooldown(task.TaskId, task.TaskTipsType)) return;

            switch (task.TaskTipsType)
            {
                case TaskModel.TIP_TRAIN_PARTNER: // 25
                    DoTrainPartner(task);
                    break;

                case 23: // TIP_TRAIN_MOUNT
                    OutWardController.Instance.StarUp(1);
                    GameLog.Info("Task", "autopilot: task={0} tips=23 action=OutWard.StarUp(1)", task.TaskId);
                    break;

                case 90: // TIP_MOUNT_LEVEL(id=type_id:1坐骑/2同修)
                    if (task.Id == 1) { OutWardController.Instance.LvUp(1); GameLog.Info("Task", "autopilot: task={0} tips=90 action=OutWard.LvUp(1)", task.TaskId); }
                    else if (task.Id == 2) { OutWardController.Instance.LvUp(2); GameLog.Info("Task", "autopilot: task={0} tips=90 action=OutWard.LvUp(2)", task.TaskId); }
                    else GameLog.Info("Task", "autopilot: task={0} tips=90 id={1} 未知子类型,跳过", task.TaskId, task.Id);
                    break;

                case 24: // 翼影
                    OutWardController.Instance.StarUpGeneric(3);
                    GameLog.Info("Task", "autopilot: task={0} tips=24 action=OutWard.StarUpGeneric(3)", task.TaskId);
                    break;
                case 92: // 圣器/古法符相
                    OutWardController.Instance.StarUpGeneric(4);
                    GameLog.Info("Task", "autopilot: task={0} tips=92 action=OutWard.StarUpGeneric(4)", task.TaskId);
                    break;
                case 41: // 神兵
                    OutWardController.Instance.StarUpGeneric(5);
                    GameLog.Info("Task", "autopilot: task={0} tips=41 action=OutWard.StarUpGeneric(5)", task.TaskId);
                    break;

                case TaskModel.TIP_SUIT_CLT: // 84
                    DoSuitCollect(task);
                    break;

                case TaskModel.TIP_AWARD_LV_GIFT: // 54
                    RushGiftController.Instance.Receive(task.Id > 0 ? task.Id : 35);
                    GameLog.Info("Task", "autopilot: task={0} tips=54 action=RushGift.Receive({1})", task.TaskId, task.Id > 0 ? task.Id : 35);
                    break;

                case TaskModel.TIP_OPEN_FUNCTION: // 81
                    TempleAwakenController.Instance.FinishInitial();
                    GameLog.Info("Task", "autopilot: task={0} tips=81 action=TempleAwaken.FinishInitial()", task.TaskId);
                    break;

                case TaskModel.TIP_ACTIVE_SOAP: // 89
                    DoGuBaoActivate(task);
                    break;

                case TaskModel.TIP_RUNE_NUM: // 33
                    DoRuneWearOrRequestBag(task, "33");
                    break;

                case 50: // TIP_RUNE_LV_SUM
                    DoRuneUpgradeOrWear(task);
                    break;

                case TaskModel.TIP_JOIN_GUILD: // 14
                    GuildJoinController.Instance.Create("神霄阁");
                    GameLog.Info("Task", "autopilot: task={0} tips=14 action=GuildJoin.Create(神霄阁)", task.TaskId);
                    break;

                case TaskModel.TIP_FIN_DUN_TYPE: // 9
                case TaskModel.TIP_DUNGEON_LEVEL: // 57
                    DoDungeonEnter(task);
                    break;

                case 91: // TIP_AFK_RECEIVE
                    OnHookController.Instance.Receive();
                    GameLog.Info("Task", "autopilot: task={0} tips=91 action=OnHook.Receive()", task.TaskId);
                    break;

                case 48: // TIP_EQUIP_STONE_NUM
                    DoEquipStone(task);
                    break;

                case 73: // TIP_RED_EQUIP_COMBINE
                    DoCompose(task);
                    break;

                case 35: // TIP_JOIN_JCC
                case 63: // TIP_KILL_BOSS_ID(大妖挑战)
                    GameLog.Warn("Task", "autopilot: task={0} tips={1} 服务端断链/未接真实入口,跳过(见 UNPORTED_TIP_SYSTEM/壳警示)", task.TaskId, task.TaskTipsType);
                    break;

                case TaskModel.TIP_LV: // 27
                    GameLog.Info("Task", "autopilot: task={0} tips=27 升级型,无动作(杀怪经验自然推进)", task.TaskId);
                    break;

                default:
                    // 其余类型(击杀/采集/道具/找NPC/副本通关等)已由 FindNextAutoFightTask/DoTask 正常驱动,不在本代行器范围。
                    break;
            }
        }

        // ---- 各 tips 动作实现 ----

        private static void DoTrainPartner(TaskVo task)
        {
            List<PartnerModel.CompanionVo> companions = PartnerModel.Instance.Companions;
            if (companions == null || companions.Count == 0)
            {
                // PartnerController 无公开的"请求 14202 列表"方法(仅在其私有 OnGameStart 里发一次,
                // 进游戏时已发过);此处不越权改 PartnerController,如实记录 blocker 并跳过等下一 tick。
                GameLog.Warn("Task", "autopilot: task={0} tips=25 Companions 为空且无公开的 14202 请求方法(PartnerController.OnGameStart 已发过一次),跳过等待", task.TaskId);
                return;
            }

            PartnerModel.CompanionVo first = companions[0];
            PartnerController.Instance.Train(first.CompanionId);
            GameLog.Info("Task", "autopilot: task={0} tips=25 action=Partner.Train({1})", task.TaskId, first.CompanionId);
        }

        private static void DoSuitCollect(TaskVo task)
        {
            int suitId = task.Id > 0 ? task.Id : 1;
            int nextStage = SuitCollectModel.Instance.GetCurStage(suitId) + 1;
            SuitCollectController.Instance.Activate(suitId, nextStage);
            GameLog.Info("Task", "autopilot: task={0} tips=84 action=SuitCollect.Activate({1},{2})", task.TaskId, suitId, nextStage);
        }

        private static void DoGuBaoActivate(TaskVo task)
        {
            int soapId = task.Id > 0 ? task.Id : 10001;
            foreach (int debrisId in GuBaoDebrisIds)
            {
                if (GuBaoModel.Instance.IsDebrisActive(soapId, debrisId)) continue;
                GuBaoController.Instance.Activate(soapId, debrisId);
                GameLog.Info("Task", "autopilot: task={0} tips=89 action=GuBao.Activate({1},{2})", task.TaskId, soapId, debrisId);
                return;
            }
            GameLog.Info("Task", "autopilot: task={0} tips=89 soap={1} 两片碎片均已激活,等服务端推进", task.TaskId, soapId);
        }

        private static void DoRuneWearOrRequestBag(TaskVo task, string tipsTag)
        {
            List<RuneModel.BagGoodsVo> bag = RuneModel.Instance.RuneBagGoods;
            if (bag != null && bag.Count > 0)
            {
                long goodsId = bag[0].GoodsId;
                RuneController.Instance.Wear(1, goodsId);
                GameLog.Info("Task", "autopilot: task={0} tips={1} action=Rune.Wear(1,{2})", task.TaskId, tipsTag, goodsId);
            }
            else
            {
                RuneController.Instance.RequestRuneBag();
                GameLog.Info("Task", "autopilot: task={0} tips={1} action=Rune.RequestRuneBag()(符文背包为空,先请求)", task.TaskId, tipsTag);
            }
        }

        private static void DoRuneUpgradeOrWear(TaskVo task)
        {
            RuneModel.SlotVo slot = RuneModel.Instance.GetSlot(1);
            if (slot != null && slot.IsWorn)
            {
                RuneController.Instance.Upgrade(slot.GoodsId);
                GameLog.Info("Task", "autopilot: task={0} tips=50 action=Rune.Upgrade({1})", task.TaskId, slot.GoodsId);
            }
            else
            {
                DoRuneWearOrRequestBag(task, "50");
            }
        }

        private static void DoDungeonEnter(TaskVo task)
        {
            // DungeonConfigs 只支持按 dun_id 单点查询(GetType(id)等),无"按 type 找最低 id"的枚举 API,
            // 且此处不允许为了本代行器去改 DungeonConfigs(红线:除 TaskController 2 行外不改既有文件)。
            // 唯一有把握的真实映射:御魂本 DUN_TYPE=12 → dun_id 12001(DungeonController/DungeonModel 类注释
            // "config_dungeon.type=12,dun_id 12001~" 反复印证)。其余 DUN_TYPE 无法在不臆造的前提下解出 id,跳过。
            if (task.Id == 12)
            {
                DungeonController.Instance.Enter(12001);
                GameLog.Info("Task", "autopilot: task={0} tips={1} action=Dungeon.Enter(12001)(DUN_TYPE=12 御魂本)", task.TaskId, task.TaskTipsType);
                return;
            }

            GameLog.Warn("Task", "autopilot: task={0} tips={1} DUN_TYPE={2} 无法解出 dun_id(DungeonConfigs 无按类型枚举 API,不臆造),跳过",
                task.TaskId, task.TaskTipsType, task.Id);
        }

        private static void DoEquipStone(TaskVo task)
        {
            foreach (BagGoods g in BagModel.Instance.BagGoodsList)
            {
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(g.TypeId);
                if (basic == null || basic.Type != GOODS_TYPE_STONE) continue;
                EquipStoneController.Instance.SetStone(1, 1, g.GoodsId);
                GameLog.Info("Task", "autopilot: task={0} tips=48 action=EquipStone.SetStone(1,1,{1})", task.TaskId, g.GoodsId);
                return;
            }
            GameLog.Warn("Task", "autopilot: task={0} tips=48 背包无 type={1} 宝石,材料不足,跳过(冷却防刷)", task.TaskId, GOODS_TYPE_STONE);
        }

        private static void DoCompose(TaskVo task)
        {
            List<ComposeConfigs.Rule> rules = ComposeConfigs.GetEquipRules(1);
            if (rules == null || rules.Count == 0)
            {
                GameLog.Warn("Task", "autopilot: task={0} tips=73 config_goods_compose 无 type==2 规则,跳过", task.TaskId);
                return;
            }

            ComposeConfigs.Rule rule = rules[0];
            var regulars = new List<long>();
            foreach (ComposeConfigs.MatEntry mat in rule.RegularMat)
            {
                long found = 0;
                foreach (BagGoods g in BagModel.Instance.BagGoodsList)
                {
                    if (g.TypeId == mat.TypeId) { found = g.GoodsId; break; }
                }
                if (found <= 0)
                {
                    GameLog.Warn("Task", "autopilot: task={0} tips=73 rule={1} 材料不足(缺 type_id={2}),跳过(冷却防刷)",
                        task.TaskId, rule.Id, mat.TypeId);
                    return;
                }
                regulars.Add(found);
            }

            ComposeController.Instance.Compose(rule.Id, regulars, new List<long>());
            GameLog.Info("Task", "autopilot: task={0} tips=73 action=Compose.Compose(rule={1},regulars={2})", task.TaskId, rule.Id, regulars.Count);
        }

        // ---- 冷却 ----

        private static bool TryConsumeCooldown(int taskId, int tipsType)
        {
            long key = (long)taskId * 1000 + tipsType;
            double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
            if (_lastActionAt.TryGetValue(key, out double until) && now < until) return false;
            _lastActionAt[key] = now + ACTION_COOLDOWN_SEC;
            return true;
        }
    }
}
