using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Dungeon
{
    /// <summary>
    /// 通用副本(pt_610,老端 BaseDungeonController.ts/BaseDungeonModel.ts)数据层。
    /// 御魂本(config_dungeon.type=12,dun_id 12001~)走这套家族协议:61001 进入/61002 退出(已在
    /// AutoBrushController 注册,复用)/61003 结算推送(对标老端 61003"通用结算界面",非 61013——
    /// 61013 在老端源码里从未注册处理器,desc 写的是"结算界面加好友/邀请公会/积分展示"社交附加功能,
    /// 与 61003 result 字段"跟61003一致"仅注释关联;结算真正入口按 675/767 行 BaseDungeonController 实证为 61003)/
    /// 61020 副本状态(次数/进度)。
    ///
    /// 轮9 副本家族补全一期:61004 副本信息/61005·61030 波次/61007·61019 坐标事件状态机/61011 助战次数/
    /// 61018 退出倒计时/61021 购买/61022 扫荡/61023 时间评分/61025·61026 鼓舞/61044 经验本面板推送/
    /// 61045 冷却时间/61046 邀请发送者原始消息/61048 双方邀请状态/61050 神纹最佳记录/61051 阶段奖励领取情况/
    /// 61053 快速出怪权威状态/
    /// 61120·61121 资源本一键与次数。
    /// 周本(50801/50802)是独立数据线,见 <see cref="PolarModel"/>——勿塞进 DunStatesByType(r9 侦察结论)。
    /// </summary>
    public sealed class DungeonModel
    {
        public static readonly DungeonModel Instance = new DungeonModel();
        private DungeonModel() { }

        // ===================================================================================
        // DUN_TYPE 常量(对标老端 BaseDungeonModel.ts:4524 DUN_TYPE 全集;只列本轮用到的)
        // ===================================================================================

        public const int TYPE_MATERIAL_COPPER = 2;   // 金币副本(新资源本合流)
        public const int TYPE_EQUIP = 5;             // 装备副本
        public const int TYPE_SOUL = 8;              // (老端已注释废弃,仅购买次数共享组保留)
        public const int TYPE_VIP_PERSON_BOSS = 10;  // 专属大妖
        public const int TYPE_RUNE = 12;             // 御魂本(灵魄)
        public const int TYPE_MARRIAGE = 13;         // 姻缘副本
        public const int TYPE_MATERIAL_MOUNT = 18;   // 剑魄同修副本(新资源本合流)
        public const int TYPE_MATERIAL_PARTNER = 19; // 伙伴材料本(新资源本合流)
        public const int TYPE_EXP = 20;              // 经验副本
        public const int TYPE_MATERIAL_WING = 22;    // 翅膀材料本(新资源本合流)
        public const int TYPE_UNREAL = 26;           // 幻饰
        public const int TYPE_HEART = 31;            // 魂殿觉醒
        public const int TYPE_DRAGON = 32;           // 神纹副本
        public const int TYPE_ADVANCED_EXP = 34;     // 高级经验副本
        public const int TYPE_SINGLE_RANK = 35;      // 天境副本(跨服个人排行)
        public const int TYPE_POLAR = 36;            // 周本(极·boss,独立 508xx 数据线)
        public const int TYPE_PARTNER = 37;          // 神巫副本
        public const int TYPE_SENTIENT_ACT = 41;     // 诸天玄门
        public const int TYPE_NEW_MOUNT = 42;        // 新资源本·坐骑
        public const int TYPE_NEW_WING = 43;         // 新资源本·翅膀
        public const int TYPE_NEW_AMULET = 44;       // 新资源本·古法符相
        public const int TYPE_NEW_WEAPON = 45;       // 新资源本·神兵
        public const int TYPE_NEW_BACK_ORNAMENT = 46;// 新资源本·背饰

        /// <summary>姻缘本 dun_id(对标老端 MarriageDefine.DUN_ID;61021 error_code==6100043 专文案判定用)。</summary>
        public const int MARRIAGE_DUN_ID = 13001;
        /// <summary>帮派经验本 dun_id(对标老端 BaseDungeonModel.GUILD_EXP_ID;61025 鼓舞加成 ×5/×10 分流用)。</summary>
        public const int GUILD_EXP_ID = 20002;
        /// <summary>扫荡券物品 id(对标老端 SWEEPING_GOODS_ID=38040001;扫荡 UI 预检库存用,本轮仅存常量)。</summary>
        public const int SWEEPING_GOODS_ID = 38040001;

        /// <summary>loading 类型白名单(对标老端 DungeonLoadindType,BaseDungeonModel.ts:4624):这些类型
        /// 服务端进副本后会**主动推 61004**;不在名单内的类型 61001 成功后客户端显式补发空参 61004。</summary>
        private static readonly int[] LoadingDunTypes =
            { TYPE_RUNE, TYPE_PARTNER, TYPE_DRAGON, TYPE_HEART, TYPE_EQUIP, TYPE_POLAR, TYPE_SINGLE_RANK };

        public static bool IsLoadingDunType(int dunType)
        {
            foreach (int t in LoadingDunTypes)
                if (t == dunType) return true;
            return false;
        }

        /// <summary>61021 购买成功后全组共享一个 vip_count 的类型组(对标老端 61021 handler switch,
        /// BaseDungeonController.ts:1263-1280:NEW_*/Material_*/Unreal/Soul/AdvancedExp)。</summary>
        private static readonly int[] SharedVipCountTypes =
        {
            TYPE_NEW_AMULET, TYPE_NEW_MOUNT, TYPE_NEW_WEAPON, TYPE_NEW_WING, TYPE_NEW_BACK_ORNAMENT,
            TYPE_MATERIAL_COPPER, TYPE_MATERIAL_MOUNT, TYPE_MATERIAL_PARTNER, TYPE_MATERIAL_WING,
            TYPE_UNREAL, TYPE_SOUL, TYPE_ADVANCED_EXP,
        };

        public static bool IsSharedVipCountType(int dunType)
        {
            foreach (int t in SharedVipCountTypes)
                if (t == dunType) return true;
            return false;
        }

        /// <summary>61022 扫荡成功后全组 daily_count += auto_num 的类型组(对标老端 61022 handler switch,
        /// BaseDungeonController.ts:1391-1404:NEW_* + Material_*;⚠比 SharedVipCountTypes 少 Unreal/Soul/AdvancedExp)。</summary>
        private static readonly int[] SweepGroupCountTypes =
        {
            TYPE_NEW_AMULET, TYPE_NEW_MOUNT, TYPE_NEW_WEAPON, TYPE_NEW_WING, TYPE_NEW_BACK_ORNAMENT,
            TYPE_MATERIAL_COPPER, TYPE_MATERIAL_MOUNT, TYPE_MATERIAL_PARTNER, TYPE_MATERIAL_WING,
        };

        public static bool IsSweepGroupCountType(int dunType)
        {
            foreach (int t in SweepGroupCountTypes)
                if (t == dunType) return true;
            return false;
        }

        /// <summary>61020 触发时机白名单(对标老端 Init() init_data_dun_type_list,BaseDungeonController.ts:168-190;
        /// GAME_START/等级变化/任务推进时经 500ms 防抖批量补请求,见 DungeonController.CheckAllDunInitState)。</summary>
        public static readonly int[] InitStateDunTypes =
        {
            TYPE_RUNE, TYPE_MATERIAL_MOUNT, TYPE_MATERIAL_PARTNER, TYPE_MATERIAL_WING, TYPE_MATERIAL_COPPER,
            TYPE_EXP, TYPE_VIP_PERSON_BOSS, TYPE_EQUIP, TYPE_UNREAL, TYPE_MARRIAGE, TYPE_DRAGON,
            TYPE_ADVANCED_EXP, TYPE_SINGLE_RANK, TYPE_SOUL, TYPE_HEART, TYPE_SENTIENT_ACT,
            TYPE_NEW_AMULET, TYPE_NEW_MOUNT, TYPE_NEW_WEAPON, TYPE_NEW_WING, TYPE_NEW_BACK_ORNAMENT,
        };

        /// <summary>61020 dun_list 单项(字段名对照 ClientProtocol.json "61020" dun_list)。</summary>
        public sealed class DunState
        {
            public int DunId;
            public int DailyCount;
            public int WeeklyCount;
            public int PermanentCount;
            public int ResetCount;
            public int VipCount;
            public int AddCount;
            public bool IsSweep;
        }

        /// <summary>按 dun_type 分组的副本状态(61020 回包一次带一个 dun_type 的 dun_list)。</summary>
        public readonly Dictionary<int, List<DunState>> DunStatesByType = new Dictionary<int, List<DunState>>();

        /// <summary>61003 结算推送最近一次结果(对标老端 SetDungeonResultInfo);result==1 成功。</summary>
        public int LastSettleResult { get; private set; }

        /// <summary>结算奖励摘要(typeId 经 GoodsModel.GetMappingTypeId 映射前的原始 style/typeId,num 已合并 reward_list)。</summary>
        public readonly List<(int typeId, long num)> LastSettleRewards = new List<(int typeId, long num)>();

        /// <summary>当前所在副本 dun_id(0=不在副本)。61001 成功后置位,61002 退出/61003 结算失败后按需清。</summary>
        public int InDungeonId { get; private set; }

        public bool HasData { get; private set; }

        /// <summary>是否收到过 61044 权威面板快照；全零字段仍视为已收到。</summary>
        public bool HasExpDungeonInfo { get; private set; }
        public ushort ExpDungeonKillCount { get; private set; }
        public ulong ExpDungeonTotalExp { get; private set; }

        /// <summary>是否实际收到过 61046 发送者消息；不代表邀请成功或完成。</summary>
        public bool HasInviteResponse { get; private set; }
        public string InviteResponseMessage { get; private set; }

        public sealed class InviteStateEntry
        {
            public byte Type;
            public ulong RoleId;
            public FigureProto Figure;
        }

        public sealed class InviteStateSnapshot
        {
            public uint Code;
            public List<InviteStateEntry> List;
            public uint DunId;
        }

        /// <summary>是否收到过 61048；空列表和全零字段仍是合法完整快照。</summary>
        public bool HasInviteState { get; private set; }
        public InviteStateSnapshot LastInviteState { get; private set; }

        public sealed class DragonBestRecordRole
        {
            public ulong RoleId;
            public string Name;
            public uint Power;
            public uint ServerNum;
            public uint ServerId;
        }

        public sealed class DragonBestRecordSnapshot
        {
            public uint DunId;
            public byte Wave;
            public uint MyTime;
            public uint BestTime;
            public List<DragonBestRecordRole> RoleList;
        }

        /// <summary>是否收到过 61050；空角色表和全零字段仍是合法完整快照。</summary>
        public bool HasDragonBestRecord { get; private set; }
        public DragonBestRecordSnapshot LastDragonBestRecord { get; private set; }

        public sealed class DragonStageRewardSnapshot
        {
            public byte HistoryWave;
            public List<byte> ClaimedWaves;
        }

        /// <summary>61051 按 dun_id 保存；TryGet=false 表示从未收到，同键回包整体替换。</summary>
        public readonly Dictionary<uint, DragonStageRewardSnapshot> DragonStageRewardsByDunId =
            new Dictionary<uint, DragonStageRewardSnapshot>();

        /// <summary>是否收到过 61053；全零字段仍是合法完整快照。</summary>
        public bool HasDragonQuickInfo { get; private set; }
        public ushort QuickCount { get; private set; }
        public ushort TotalQuickCount { get; private set; }
        public uint NextQuickTime { get; private set; }

        public sealed class DragonSkillInfoEntry
        {
            public uint SkillId;
            public ushort Num;
        }

        /// <summary>是否收到过 61055；列表保留 wire 原序和重复项，空表仍是合法完整快照。</summary>
        public bool HasDragonSkillInfo { get; private set; }
        public List<DragonSkillInfoEntry> DragonSkillInfo { get; private set; } =
            new List<DragonSkillInfoEntry>();

        public void ApplyExpDungeonInfo(ushort killCount, ulong totalExp)
        {
            HasExpDungeonInfo = true;
            ExpDungeonKillCount = killCount;
            ExpDungeonTotalExp = totalExp;
        }

        public void ApplyInviteResponse(string message)
        {
            HasInviteResponse = true;
            InviteResponseMessage = message;
        }

        public void ApplyInviteState(uint code, List<InviteStateEntry> list, uint dunId)
        {
            HasInviteState = true;
            LastInviteState = new InviteStateSnapshot
            {
                Code = code,
                List = list ?? new List<InviteStateEntry>(),
                DunId = dunId,
            };
        }

        public void ApplyDragonBestRecord(uint dunId, byte wave, uint myTime, uint bestTime,
            List<DragonBestRecordRole> roles)
        {
            HasDragonBestRecord = true;
            LastDragonBestRecord = new DragonBestRecordSnapshot
            {
                DunId = dunId,
                Wave = wave,
                MyTime = myTime,
                BestTime = bestTime,
                RoleList = roles ?? new List<DragonBestRecordRole>(),
            };
        }

        public void ApplyDragonStageReward(uint dunId, byte historyWave, List<byte> claimedWaves)
        {
            DragonStageRewardsByDunId[dunId] = new DragonStageRewardSnapshot
            {
                HistoryWave = historyWave,
                ClaimedWaves = claimedWaves ?? new List<byte>(),
            };
        }

        public bool TryGetDragonStageReward(uint dunId, out DragonStageRewardSnapshot snapshot) =>
            DragonStageRewardsByDunId.TryGetValue(dunId, out snapshot);

        public void ApplyDragonQuickInfo(ushort quickCount, ushort totalQuickCount, uint nextQuickTime)
        {
            HasDragonQuickInfo = true;
            QuickCount = quickCount;
            TotalQuickCount = totalQuickCount;
            NextQuickTime = nextQuickTime;
        }

        public void ApplyDragonSkillInfo(List<DragonSkillInfoEntry> skills)
        {
            HasDragonSkillInfo = true;
            DragonSkillInfo = skills ?? new List<DragonSkillInfoEntry>();
        }

        /// <summary>61045 按 dun_id 保存的服务器绝对冷却结束时间；0 也是合法回包。</summary>
        public readonly Dictionary<uint, uint> CooldownEndTimes = new Dictionary<uint, uint>();

        public void ApplyCooldown(uint dunId, uint nextTime)
        {
            CooldownEndTimes[dunId] = nextTime;
        }

        public bool TryGetCooldown(uint dunId, out uint nextTime) =>
            CooldownEndTimes.TryGetValue(dunId, out nextTime);

        /// <summary>61001 进入成功回包套值(对标老端 on61001 error_code==1 分支)。</summary>
        public void Apply61001(int dunId)
        {
            InDungeonId = dunId;
            HasData = true;
        }

        /// <summary>61020 副本状态回包套值(对标老端 GetDungeonInfo/SetDungeonInfo,按 dun_type 整表替换)。</summary>
        public void Apply61020(int dunType, List<DunState> list)
        {
            DunStatesByType[dunType] = list ?? new List<DunState>();
            HasData = true;
        }

        /// <summary>61003 结算推送套值(对标老端 SetDungeonResultInfo)。rewards 由控制器按 reward_list 汇总传入。</summary>
        public void ApplySettle(int result, List<(int typeId, long num)> rewards)
        {
            LastSettleResult = result;
            LastSettleRewards.Clear();
            if (rewards != null) LastSettleRewards.AddRange(rewards);
            if (result != 1) InDungeonId = 0;   // 失败:老端随即弹失败面板,副本记录已不在该层,标记不在副本内(实际以 61002/场景切换为准)
            HasData = true;
        }

        public DunState GetState(int dunType, int dunId)
        {
            if (!DunStatesByType.TryGetValue(dunType, out List<DunState> list) || list == null) return null;
            return list.Find(s => s.DunId == dunId);
        }

        // ===================================================================================
        // 61004 副本信息(对标老端 SetDungeonSceneMsg → dungeon_scene_scmd)
        // ===================================================================================

        public sealed class SceneInfoVo
        {
            public int StartTime;
            public long StartTimeMs;
            public int EndTime;
            public int Level;
            public int LevelEndTime;
            public long OwnerId;
            public int WaveNum;
        }

        /// <summary>最近一次 61004 副本信息(null=尚未收到)。</summary>
        public SceneInfoVo SceneInfo { get; private set; }

        public void SetSceneInfo(SceneInfoVo vo)
        {
            SceneInfo = vo;
        }

        // ===================================================================================
        // 61005/61030 波次(对标老端 curr_wave_type/curr_wave_num + get_next_wave_time)
        // ===================================================================================

        public int CurrWaveType { get; private set; }
        /// <summary>当前波数(对标老端 curr_wave_num;61002 退出成功后重置为 1)。</summary>
        public int CurrWaveNum { get; private set; } = 1;
        /// <summary>61030 下一波怪物生成时间(服务器时间戳,0=未知)。</summary>
        public int NextWaveTime { get; private set; }

        public void SetWaveInfo(int waveType, int waveNum)
        {
            CurrWaveType = waveType;
            CurrWaveNum = waveNum;
        }

        public void SetNextWaveTime(int waveNum, int time)
        {
            CurrWaveNum = waveNum;
            NextWaveTime = time;
        }

        /// <summary>61002 退出成功重置波数(对标老端 61002 handler:curr_wave_num=1)。</summary>
        public void ResetWaveNum()
        {
            CurrWaveNum = 1;
        }

        // ===================================================================================
        // 61007/61019 坐标事件状态机(对标老端 role_pos_event_list;trigger_state 1未触发/2触发中/3完成)
        // ===================================================================================

        public sealed class PosEventVo
        {
            public int SceneId;
            public int PosX;
            public int PosY;
            public int XRange;
            public int YRange;
            public int Order;
            /// <summary>1=未触发 2=正在触发(已发 61007 等回执) 3=触发完成。</summary>
            public int TriggerState = 1;
        }

        /// <summary>坐标触发事件表。老端由场景元素配置(erlang term "{role_pos,x,y,xr,yr,order}")解析填充
        /// (BaseDungeonModel.ts:4370-4410);Unity 场景元素配置解析未移植,本表由未来场景层经
        /// <see cref="AddPosEvent"/> 填充,状态机先落地(TODO:场景元素配置源接线)。</summary>
        public readonly List<PosEventVo> RolePosEventList = new List<PosEventVo>();

        /// <summary>登记一个坐标触发事件(按 order 升序插入,对标老端 role_pos_event_list.sort(order))。</summary>
        public void AddPosEvent(PosEventVo vo)
        {
            if (vo == null) return;
            RolePosEventList.Add(vo);
            RolePosEventList.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        /// <summary>主角移动检查(对标老端 onMainRoleMoveHandler,DungeonFightSceneView.ts:296-313):
        /// 当前场景内 trigger_state==1 且主角进入 x/y_range 的事件置 2(触发中)并返回该事件
        /// (调用方随即发 61007,坐标用**事件目标点**而非主角位置——老端 TriggerFlushMonster(tx,ty))。</summary>
        public PosEventVo TryEnterPosEvent(int sceneId, int mx, int my)
        {
            foreach (PosEventVo vo in RolePosEventList)
            {
                if (vo.SceneId != sceneId || vo.TriggerState != 1) continue;
                if (System.Math.Abs(vo.PosX - mx) <= vo.XRange && System.Math.Abs(vo.PosY - my) <= vo.YRange)
                {
                    vo.TriggerState = 2;
                    return vo;
                }
            }
            return null;
        }

        /// <summary>61007 回执落地(对标老端 SuccessTriggerRolePos,BaseDungeonModel.ts:4430-4440):
        /// 命中范围内的事件置 3(完成);其余若曾是 2(触发中)回退 1(未触发)。</summary>
        public void SuccessTriggerRolePos(int x, int y)
        {
            foreach (PosEventVo vo in RolePosEventList)
            {
                if (System.Math.Abs(x - vo.PosX) <= vo.XRange && System.Math.Abs(y - vo.PosY) <= vo.YRange)
                    vo.TriggerState = 3;
                else if (vo.TriggerState == 2)
                    vo.TriggerState = 1;
            }
        }

        /// <summary>61019 触发情况表对账(对标老端 ResetPosEventList,BaseDungeonModel.ts:4413-4426):
        /// 服务端已记录的坐标点逐一比对,命中的置 trigger_state=3,避免重进场景重复触发。</summary>
        public void ResetPosEventList(List<(int x, int y)> serverRecords)
        {
            if (serverRecords == null || serverRecords.Count == 0) return;
            foreach (PosEventVo vo in RolePosEventList)
            {
                foreach ((int x, int y) rec in serverRecords)
                {
                    if (vo.PosX == rec.x && vo.PosY == rec.y) { vo.TriggerState = 3; break; }
                }
            }
        }

        /// <summary>清副本内临时状态(对标老端 ClearDungeonInfo:role_pos_event_list/refresh_monster_info/curr_wave_type)。</summary>
        public void ClearDungeonInfo()
        {
            RolePosEventList.Clear();
            CurrWaveType = 0;
            SceneInfo = null;
            NextWaveTime = 0;
            ExitEndTime = 0;
        }

        // ===================================================================================
        // 61011 助战剩余次数(对标老端 SetDungeonHelpData → dungeon_help_data_)
        // ===================================================================================

        private readonly Dictionary<int, int> _helpCounts = new Dictionary<int, int>();

        public int GetHelpCount(int dunId) => _helpCounts.TryGetValue(dunId, out int v) ? v : 0;

        public void SetHelpCount(int dunId, int leftHelpCount)
        {
            _helpCounts[dunId] = leftHelpCount;
        }

        // ===================================================================================
        // 61018 退出倒计时(对标老端 UPDATE_DUNGEON_END_TIME 唯一数据源)
        // ===================================================================================

        /// <summary>副本剩余可停留时间终点(服务器时间戳;0=无倒计时。仅 61018 type==1 才写)。</summary>
        public int ExitEndTime { get; private set; }

        public void SetExitEndTime(int endTime)
        {
            ExitEndTime = endTime;
        }

        // ===================================================================================
        // 61023 时间评分状态(对标老端 NOW_TIME_SCORE_STATE,装备本星级评分随时间变化)
        // ===================================================================================

        public sealed class ScoreStateVo
        {
            public int CurScore;
            public int NextScore;
            public int ChangeTime;
        }

        public ScoreStateVo ScoreState { get; private set; }

        public void SetScoreState(int curScore, int nextScore, int changeTime)
        {
            ScoreState = new ScoreStateVo { CurScore = curScore, NextScore = nextScore, ChangeTime = changeTime };
        }

        // ===================================================================================
        // 61025/61026 鼓舞(对标老端 SetInspireInfo → inspire_coin/gold;伤害加成 = (coin+gold)×5或10%)
        // ===================================================================================

        public int InspiritCoinCount { get; private set; }
        public int InspiritGoldCount { get; private set; }

        public void SetInspiritInfo(int coinCount, int goldCount)
        {
            InspiritCoinCount = coinCount;
            InspiritGoldCount = goldCount;
        }

        /// <summary>鼓舞伤害加成百分比(对标老端 61025 成功分支:帮派经验本(GUILD_EXP_ID)每次 ×5%,其余 ×10%)。</summary>
        public int GetInspiritBonusPercent(int curDunId)
        {
            int per = curDunId == GUILD_EXP_ID ? 5 : 10;
            return (InspiritCoinCount + InspiritGoldCount) * per;
        }

        // ===================================================================================
        // 61121 资源副本次数(对标老端 dungeon_num_data_/SaveDungeonNumData)
        // ===================================================================================

        public sealed class ResourceCountVo
        {
            public int SweepCount;
            public int ChallengeCount;
        }

        private readonly Dictionary<int, ResourceCountVo> _resourceCounts = new Dictionary<int, ResourceCountVo>();

        public ResourceCountVo GetResourceCount(int dunType) =>
            _resourceCounts.TryGetValue(dunType, out ResourceCountVo v) ? v : null;

        /// <summary>对标老端 RequestDungeonNum(0) 先清表再全量重建。</summary>
        public void ClearResourceCounts() => _resourceCounts.Clear();

        public void SetResourceCount(int dunType, int sweepCount, int challengeCount)
        {
            _resourceCounts[dunType] = new ResourceCountVo { SweepCount = sweepCount, ChallengeCount = challengeCount };
        }

        // ===================================================================================
        // 61020 触发时机的每类型 init 状态(对标老端 GetDunInitState/SetDunInitState/ResetDunInitState)
        // ===================================================================================

        private readonly HashSet<int> _dunInitState = new HashSet<int>();

        public bool GetDunInitState(int dunType) => _dunInitState.Contains(dunType);
        public void SetDunInitState(int dunType) => _dunInitState.Add(dunType);
        /// <summary>对标老端 DAY_CHANGE/HOUR_REFRESH 的 ResetDunInitState(全类型待重拉)。</summary>
        public void ResetDunInitState() => _dunInitState.Clear();

        public void Clear()
        {
            DunStatesByType.Clear();
            LastSettleRewards.Clear();
            LastSettleResult = 0;
            InDungeonId = 0;
            HasData = false;
            HasExpDungeonInfo = false;
            ExpDungeonKillCount = 0;
            ExpDungeonTotalExp = 0;
            HasInviteResponse = false;
            InviteResponseMessage = null;
            HasInviteState = false;
            LastInviteState = null;
            HasDragonBestRecord = false;
            LastDragonBestRecord = null;
            DragonStageRewardsByDunId.Clear();
            HasDragonQuickInfo = false;
            QuickCount = 0;
            TotalQuickCount = 0;
            NextQuickTime = 0;
            HasDragonSkillInfo = false;
            DragonSkillInfo.Clear();
            SceneInfo = null;
            CurrWaveType = 0;
            CurrWaveNum = 1;
            NextWaveTime = 0;
            RolePosEventList.Clear();
            _helpCounts.Clear();
            ExitEndTime = 0;
            ScoreState = null;
            InspiritCoinCount = 0;
            InspiritGoldCount = 0;
            _resourceCounts.Clear();
            _dunInitState.Clear();
            CooldownEndTimes.Clear();
        }
    }

    /// <summary>
    /// config_dungeon 读取器(数字索引键,39 列;列序权威来源 yu_client cdn/resource/config/server/
    /// config_table_default.json 的 config_dungeon 字段名列表):
    ///   · "0"  = id(主键)
    ///   · "1"  = name(副本名)
    ///   · "2"  = type(副本大类;御魂本=12)
    ///   · "10" = scene_id(场景 id;12001~12003 实测=2005)
    ///   · "20" = condition(进入条件,erlang term 字符串,原样存不解析——用途留后接 ErlangParser)
    /// 表经 ClientConfigSync 从 yu_client cdn 同步进 Assets/GameRes/resource/config/server/config_dungeon.json。
    /// </summary>
    public static class DungeonConfigs
    {
        private static JObject _dungeon;

        public static bool IsLoaded => _dungeon != null;

        public static async Task EnsureLoaded()
        {
            if (_dungeon != null) return;
            string key = GameResPath.GetServerConfigPath("config_dungeon");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Dungeon", "missing config_dungeon: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _dungeon = new JObject();
                return;
            }
            _dungeon = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("Dungeon", "config_dungeon={0}", _dungeon.Count);
        }

        /// <summary>副本名(列 1);缺表/缺项降级 "副本{id}"(标出而非臆造)。</summary>
        public static string GetName(int dunId)
        {
            if (_dungeon?[dunId.ToString()] is JObject obj)
            {
                string name = ReadString(obj, "1");
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return "副本" + dunId;
        }

        /// <summary>副本大类(列 2);缺表/缺项返回 0。</summary>
        public static int GetType(int dunId)
        {
            if (_dungeon?[dunId.ToString()] is JObject obj) return ReadInt(obj, "2");
            return 0;
        }

        /// <summary>场景 id(列 10)。</summary>
        public static int GetSceneId(int dunId)
        {
            if (_dungeon?[dunId.ToString()] is JObject obj) return ReadInt(obj, "10");
            return 0;
        }

        /// <summary>进入条件(列 20,erlang term 字符串原样存;解析留后接 ErlangParser)。</summary>
        public static string GetCondition(int dunId)
        {
            if (_dungeon?[dunId.ToString()] is JObject obj) return ReadString(obj, "20");
            return "";
        }

        // —— 数字索引键读取小工具(字符串/数字混排容错,同 PartnerConfigs/GoodsModel)——
        private static int ReadInt(JObject obj, string key)
        {
            Newtonsoft.Json.Linq.JToken token = obj[key];
            if (token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null) return 0;
            return token.Type == Newtonsoft.Json.Linq.JTokenType.Integer ? token.Value<int>()
                : int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            Newtonsoft.Json.Linq.JToken token = obj[key];
            return token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null ? "" : token.ToString();
        }
    }
}
