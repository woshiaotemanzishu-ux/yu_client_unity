using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Boss
{
    /// <summary>
    /// BOSS(节日大妖/节日BOSS)图标数据(对标老客户端 BossModel 中与 addIcon("51") 相关的那一小块)。
    /// 大 BOSS 系统体量庞大(本服/跨服 46xxx/47xxx 玩法协议),本期只承载"节日大妖"主界面图标 51 的显隐,
    /// 玩法(进副本/BOSS信息/掉落/复活等)一律不做。
    ///
    /// 图标 51 = 老端 CustomActivityDefine.AdvanceIcon[51]="51",由 BossModel.FeastBossActivity 驱动:
    /// 读自定义活动(FEASTBOSS,base_type=51)的 condition 时间窗——
    ///   · 处于活动时间窗内 → addIcon("51", 活动结束时间戳)(带倒计时);
    ///   · 当天有下一场未开始 → addIcon("51", 文本 "HH:MM开启")(预告);
    ///   · 都没有 → deleteIcon("51")。
    /// 门槛(是否有节日BOSS活动、时间窗)全在服务端 + 自定义活动条件里把控,本 Model 只存最终判定结果。
    /// </summary>
    public sealed class BossModel
    {
        public static readonly BossModel Instance = new BossModel();
        private BossModel() { }

        /// <summary>主界面图标类型(对标老端 CustomActivityDefine.AdvanceIcon[51]="51")。</summary>
        public const string ICON_TYPE = "51";

        /// <summary>节日大妖活动的自定义活动 base_type(对标老端 ConfigCustomActivity.ACT_ID.FEASTBOSS=51)。</summary>
        public const int FEAST_BOSS_BASE_TYPE = 51;

        /// <summary>
        /// 服务端配置时区(线上=UTC+8,按"落定线上值"做法)。活动 condition 里 time 窗的时分秒是服务端墙钟,
        /// 需把 UTC 服务器时间(TimeUtil.NowUtc)加此偏移换算成墙钟再比窗(对标老端 TimeUtil.GetZoneTime 的 server_zone)。
        /// 轮20收敛:转发 TimeUtil.SERVER_ZONE_HOURS(唯一事实源),值不变、零行为变更,保留常量名/可见性
        /// 避免改调用点(spec_serverclock_round20.md §2.3)。
        /// </summary>
        public const int SERVER_ZONE_HOURS = TimeUtil.SERVER_ZONE_HOURS;

        /// <summary>
        /// 计算节日BOSS当前三态(对标老端 BossModel.GetFeastBossTime + FeastBossActivity 的图标分支)。
        /// condition 形如 [{show_time,N},{time,[{{H,M,S},{H,M,S}},...]}];窗按服务端时区墙钟(UTC+8)判定。
        /// 返回:active=是否处于某窗内;endTime=窗结束的 unix 时间戳(倒计时用);foreshadow=当天下一场"HH:MM开启"文本(无则 null)。
        /// nowSec=原始服务器 unix 秒(TimeUtil.NowSec);活动总区间 [actStartTime, actEndTime) 之外一律无。
        /// </summary>
        public static (bool active, int endTime, string foreshadow) ComputeFeastWindow(
            string condition, int actStartTime, int actEndTime, long nowSec)
        {
            // 活动总区间外:无(对标老端 curTime>=stime && curTime<etime 的外层门)
            if (actStartTime > 0 && nowSec < actStartTime) return (false, 0, null);
            if (actEndTime > 0 && nowSec >= actEndTime) return (false, 0, null);

            IReadOnlyList<ErlangTerm> timeArr = ExtractTimeWindows(condition);
            if (timeArr == null || timeArr.Count == 0) return (false, 0, null);

            // 服务端墙钟时分秒(UTC+SERVER_ZONE_HOURS)
            DateTime zoneNow = TimeUtil.NowUtc().AddHours(SERVER_ZONE_HOURS);
            int hour = zoneNow.Hour, minute = zoneNow.Minute, second = zoneNow.Second;
            int nowAllToday = hour * 3600 + minute * 60 + second; // 今日已过秒(墙钟)

            // 1) 是否在某个窗内(对标老端 now_all>0 && now_all<=all_sce_time)
            for (int i = 0; i < timeArr.Count; i++)
            {
                if (!TryReadWindow(timeArr[i], out int sH, out _, out _, out int startSec, out int eH, out int endSec)) continue;
                if (hour >= sH && hour <= eH && nowAllToday > startSec && nowAllToday <= endSec)
                {
                    int left = endSec - nowAllToday;                 // 距窗结束剩余秒
                    return (true, (int)(nowSec + left), null);       // 倒计时到窗结束的 unix 时间戳
                }
            }

            // 2) 当天有没有下一场未开始的窗 → 预告 "HH:MM开启"(对标老端 next_open_time 分支)
            for (int i = 0; i < timeArr.Count; i++)
            {
                if (!TryReadWindow(timeArr[i], out int sH, out int sM, out _, out int startSec, out _, out _)) continue;
                if (nowAllToday < startSec)
                {
                    string mm = sM >= 10 ? sM.ToString() : "0" + sM;
                    return (false, 0, sH + ":" + mm + "开启");
                }
            }

            return (false, 0, null);
        }

        // 从 condition 里取 {time,[...]} 的窗列表(对标老端 GetFeastBossTime 遍历 condition 找 "time")。
        private static IReadOnlyList<ErlangTerm> ExtractTimeWindows(string condition)
        {
            ErlangTerm cond = ErlangParser.Parse(condition);
            if (cond?.Items == null) return null;
            foreach (ErlangTerm tup in cond.Items)
            {
                IReadOnlyList<ErlangTerm> kv = tup?.Items;
                if (kv == null || kv.Count < 2) continue;
                if (kv[0].As<string>() == "time") return kv[1]?.Items;
            }
            return null;
        }

        // 解析单个窗 {{sH,sM,sS},{eH,eM,eS}} → 起止时分秒 + 当日秒偏移。
        private static bool TryReadWindow(ErlangTerm window, out int sH, out int sM, out int sS,
            out int startSec, out int eH, out int endSec)
        {
            sH = sM = sS = startSec = eH = endSec = 0;
            IReadOnlyList<ErlangTerm> win = window?.Items;
            if (win == null || win.Count < 2) return false;
            IReadOnlyList<ErlangTerm> a = win[0]?.Items, b = win[1]?.Items;
            if (a == null || b == null || a.Count < 3 || b.Count < 3) return false;
            sH = a[0].As<int>(); sM = a[1].As<int>(); sS = a[2].As<int>();
            eH = b[0].As<int>(); int eM = b[1].As<int>(), eS = b[2].As<int>();
            startSec = sH * 3600 + sM * 60 + sS;
            endSec = eH * 3600 + eM * 60 + eS;
            return true;
        }

        // 节日BOSS图标最终判定(对标老端 FeastBossActivity 的三态):
        public bool FeastBossActive;       // 是否处于活动时间窗内(带倒计时)
        public int FeastBossEndTime;       // 活动结束时间戳(秒),AddIconAsync 的 time 入参,做倒计时
        public string FeastBossForeshadow; // 预告文本("HH:MM开启");非空表示当天有下一场未开始

        /// <summary>
        /// 写入节日BOSS图标判定(对标老端 FeastBossActivity 计算出的 [is_in_activity_time, left_time, next_open_time])。
        /// active=时间窗内、endTime=结束时间戳(倒计时用)、foreshadow=预告文本("HH:MM开启",无则传 null/空)。
        /// </summary>
        public void SetFeastBossActivity(bool active, int endTime, string foreshadow)
        {
            FeastBossActive = active;
            FeastBossEndTime = endTime;
            FeastBossForeshadow = foreshadow;
        }

        /// <summary>
        /// 入口开启状态(对标老端 FeastBossActivity:处于活动时间窗 或 有下一场预告 时才挂图标 51)。
        /// 两者皆无 → 无节日BOSS活动 → 不显示。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return FeastBossActive || !string.IsNullOrEmpty(FeastBossForeshadow);
        }

        public void Reset()
        {
            FeastBossActive = false;
            FeastBossEndTime = 0;
            FeastBossForeshadow = null;
        }

        // ============================================================================================
        // Boss 家族一期·本服核心(自动循环 轮15a)。46000 段(pt_460)+ 20025-26(采集)+ 20201-205(免战)。
        // 范围铁律:47000-47035/47101-47117/61900-61902 跨服族全部不接(留15b);
        // VIP_PERSONAL(2)/PERSONAL(8) 服务端无 check_enter_boss 匹配分支,本轮不做进入(走其他系统入口)。
        // 时钟纪律:凡涉及"服务器墙钟"的换算(免战剩余时间等)一律走 TimeUtil.NowSec() + SERVER_ZONE_HOURS,
        // 不用本地系统时间。
        // ============================================================================================

        /// <summary>
        /// boss_type 枚举(对标老端 BossModel.BossType 与服务端 boss.hrl 交叉核对,数值一致——
        /// 轮15a 只用得到这些;47000 系跨服变体是本值+1000,不在本文件出现)。
        /// </summary>
        public static class BossType
        {
            public const int World = 1;          // 旧世界boss(legacy,已被 Field 取代,config_boss_type 仍有行)
            public const int VipPersonal = 2;     // 服务端无 check_enter_boss 匹配分支,不接
            public const int Home = 3;            // boss家园(HOME)
            public const int Suit = 4;            // 蛮荒禁地(FORBIDDEN)——BossEnterView Tab3"太古妖尊"
            public const int Temple = 5;          // 遗忘神庙(TEMPLE)
            public const int Outside = 6;         // 野外(OUTSIDE)
            public const int Abyss = 7;           // 深渊/禁天妖土(ABYSS)——BossEnterView Tab1"boss之家"
            public const int Personal = 8;        // 服务端无 check_enter_boss 匹配分支,不接
            public const int Secret = 9;          // 秘境(FAIRYLAND)
            public const int Eudaemon = 10;       // 幻兽领(PHANTOM,本服;跨服"千幻蜃楼"走47000+1000,不同概念)
            public const int Feast = 11;          // 节日大妖(既有 FeastBoss 图标逻辑占用,本轮不动)
            public const int Field = 12;          // 新野外/世界boss(NEW_OUTSIDE)——BossEnterView Tab0"诸天妖帝"
            public const int FieldSpecial = 13;   // 单人无限层(SPECIAL)
            public const int HolyTerritory = 14;  // 圣域(领地),独立协议族,不属本包
            public const int Mystery = 16;        // 秘境领域/太古遗凶(DOMAIN);跨服变体是本值+1000=1016
            public const int PhantomPer = 18;      // 单人无限层变体
            public const int FieldInfinite = 19;   // 单人无限层变体(WORLD_PER)
            public const int KfGreatDemon = 20;    // 跨服秘境大妖,本轮不接(壳复用同一批 46xxx 号)
        }

        // ---- 46009 订正后类型门(rule10,轮13 权限 truthy 同款笔误订正):老端 `||` 链漏 `==`,
        // 后 7 项裸常量恒真,KILL_BOSS 对任意 boss_type 无条件触发——本端改成显式集合命中判断。
        // 轮15b 复核订正:老端 On46009 门里那句 `BossModel.BossType.mystery` 在老端常量表(BossModel.ts:143
        // `mystery: 20,//秘境领域boss 本服16 跨服20`)运行时数值其实是 20,不是 15a 沿用的 16——15a 把这门
        // 错接到了本服 BOSS_TYPE_DOMAIN(16)常量上,订正为 KfGreatDemon(20)。
        // 双收依据(服务端 write(46009) 调用点实参核对,E:\GitProject\yu_server;15b 服务端镜头复验):
        //   ·16(DOMAIN)确有真实到达路径——DOMAIN boss 经 mod_boss.erl:717/786(gm_create_domain_boss cast +
        //     boss_be_kill DOMAIN 分支)触达 lib_boss.erl:2400(cl_boss_reborn,**全类型通用**重生广播,write(46009,
        //     [BossType,...]) 的 BossType 是配置读出的变量、非字面 ?BOSS_TYPE_DOMAIN)/:2494(create_domain_special_boss),
        //     DOMAIN 路径实际携带 type=16 送达,可达为真,非死代码。
        //   ·20(KfGreatDemon)同样有真实到达路径——lib_great_demon.erl:457/504/534 三处**字面**
        //     pt_460:write(46009,[?BOSS_TYPE_KF_GREAT_DEMON,...]),跨服节点补怪/重生广播。
        // 两个类型都会真实收到 46009,故双收(16 订正保留 + 20 补上),而不是简单替换。
        private static readonly HashSet<int> KillBossNotifyTypes = new HashSet<int>
        {
            BossType.Suit, BossType.Secret, BossType.Eudaemon, BossType.Field,
            BossType.FieldInfinite, BossType.FieldSpecial, BossType.Abyss,
            BossType.Mystery,      // BOSS_TYPE_DOMAIN=16,双收依据见上
            BossType.KfGreatDemon, // BOSS_TYPE_KF_GREAT_DEMON=20,老端 mystery 常量运行时真值,本轮主收订正
        };

        public static bool ShouldNotifyKillBoss(int bossType) => KillBossNotifyTypes.Contains(bossType);

        /// <summary>46000 单条 boss_info(对标老端 boss_info[{boss_id,num,reborn_time,is_remind,auto_remind}])。</summary>
        public sealed class BossEntry
        {
            public int BossId;
            public int Num;            // 剩余可击杀数(存活判定:num>0 视为存活)
            public long RebornTime;    // 下次刷新时间戳(num==0 时展示倒计时)
            public bool IsRemind;
            public bool AutoRemind;

            /// <summary>存活判定(旧世界boss WORLD=1 用独立 Status 字段,不走这条——本轮未接 46023,
            /// World 类型如实按 Num 兜底判断,TODO 精确化)。</summary>
            public bool IsAlive => Num > 0;
        }

        /// <summary>46000 单个 boss_type 的完整快照。</summary>
        public sealed class BossTypeState
        {
            public int BossType;
            public int AllCount;       // 每日总次数上限(0=不限)
            public int Count;          // 剩余可进入次数
            public int Tired;          // 当前疲劳
            public int AllTired;       // 疲劳上限
            public int Vit;            // 体力(OUTSIDE/NEW_OUTSIDE 类型用)
            public long LastVitTime;   // 上次体力刷新时间戳
            public int CollectTimes;
            public int AllCollectTimes;
            public readonly List<BossEntry> BossList = new List<BossEntry>();
            public bool HasData;

            public BossEntry GetEntry(int bossId)
            {
                for (int i = 0; i < BossList.Count; i++)
                    if (BossList[i].BossId == bossId) return BossList[i];
                return null;
            }
        }

        /// <summary>击杀日志单条(46001)。</summary>
        public sealed class KillLogEntry
        {
            public long Time;
            public long RoleId;
            public string Name = "";
        }

        /// <summary>装备附加属性(46002/46046 掉落日志内嵌)。</summary>
        public sealed class EquipExtraAttrEntry
        {
            public int Color;
            public int TypeId;
            public int AttrId;
            public long AttrVal;
            public int PlusInterval;
            public long PlusUnit;
        }

        /// <summary>掉落日志单条(46002 本服/46046 跨服大妖共用形态,后者多 ServerId/ServerNum/Layers/Time 字段,
        /// 本轮不接 46046——跨服 kf_great_demon 委托,详见 Proto.cs 说明,此结构只服务 46002)。</summary>
        public sealed class DropLogEntry
        {
            public long Time;
            public long RoleId;
            public string Name = "";
            public int BossType;
            public int BossId;
            public int GoodsId;
            public long Num;
            public long Rating;
            public List<EquipExtraAttrEntry> EquipExtraAttr = new List<EquipExtraAttrEntry>();
            public bool IsTop;
        }

        /// <summary>Boss 体力详情(46044)。</summary>
        public sealed class VitInfo
        {
            public int Vit;
            public int MaxVit;
            public int AddVit;
            public int BackVit;
            public long LastVitTime;
            public bool HasData;
        }

        /// <summary>伤害榜-自己(46022,非拉取,伤害发生后服务端自动回)。</summary>
        public sealed class DamageRankSelf
        {
            public int SelfRank;
            public long SelfDamage;
            public string SelfName = "";
            public long Distance;
            public bool HasData;
        }

        /// <summary>伤害榜前3名(46019,500ms 防抖场景广播)。</summary>
        public sealed class DamageRankEntry
        {
            public string RoleName = "";
            public long Damage;
        }

        /// <summary>免战保护单条(20201 protect_list)。</summary>
        public sealed class WarFreeEntry
        {
            public int SceneType;
            public int ProtectTime;
            public int UseCount;
        }

        // ---- 存储:按 boss_type 多路复用(对标服务端同一套 pt_460 靠 boss_type 参数分派) ----
        private readonly Dictionary<int, BossTypeState> _boss = new Dictionary<int, BossTypeState>();
        public IReadOnlyDictionary<int, BossTypeState> AllBossStates => _boss;

        public BossTypeState GetOrCreateBossState(int bossType)
        {
            if (!_boss.TryGetValue(bossType, out BossTypeState s))
            {
                s = new BossTypeState { BossType = bossType };
                _boss[bossType] = s;
            }
            return s;
        }

        public BossTypeState GetBossState(int bossType) => _boss.TryGetValue(bossType, out BossTypeState s) ? s : null;

        /// <summary>46000 落地(全量覆盖该 boss_type 的列表,对标老端 SetBossInfo)。</summary>
        public void ApplyBossList(int bossType, int allCount, int count, int tired, int allTired,
            int vit, long lastVitTime, int collectTimes, int allCollectTimes, List<BossEntry> entries)
        {
            BossTypeState s = GetOrCreateBossState(bossType);
            s.AllCount = allCount; s.Count = count; s.Tired = tired; s.AllTired = allTired;
            s.Vit = vit; s.LastVitTime = lastVitTime; s.CollectTimes = collectTimes; s.AllCollectTimes = allCollectTimes;
            s.BossList.Clear();
            if (entries != null) s.BossList.AddRange(entries);
            s.HasData = true;
        }

        /// <summary>46009/46036 广播落地:更新场景内该 boss 的 num/reborn_time(不存在则新增,对标老端本地 upsert)。</summary>
        public void ApplyBossReborn(int bossType, int bossId, long rebornTime, int num)
        {
            BossTypeState s = GetOrCreateBossState(bossType);
            BossEntry e = s.GetEntry(bossId);
            if (e == null)
            {
                e = new BossEntry { BossId = bossId };
                s.BossList.Add(e);
            }
            e.RebornTime = rebornTime;
            e.Num = num;
        }

        public readonly List<KillLogEntry> KillLog = new List<KillLogEntry>();
        public bool HasKillLog { get; private set; }

        /// <summary>46001 落地(服务端 ?BOSS_LOG_LEN=100 硬顶,client 侧信任服务端截断,不重复裁剪)。</summary>
        public void ApplyKillLog(List<KillLogEntry> list)
        {
            KillLog.Clear();
            if (list != null) KillLog.AddRange(list);
            HasKillLog = true;
        }

        public readonly List<DropLogEntry> DropLog = new List<DropLogEntry>();
        public bool HasDropLog { get; private set; }

        public void ApplyDropLog(List<DropLogEntry> list)
        {
            DropLog.Clear();
            if (list != null) DropLog.AddRange(list);
            HasDropLog = true;
        }

        // ---- 体力(NEW_OUTSIDE/SPECIAL 类型用,46043/46044) ----
        private readonly Dictionary<int, VitInfo> _vit = new Dictionary<int, VitInfo>();
        public VitInfo GetVit(int bossType) => _vit.TryGetValue(bossType, out VitInfo v) ? v : null;

        public void ApplyVit(int bossType, int vit, int maxVit, int addVit, int backVit, long lastVitTime)
        {
            _vit[bossType] = new VitInfo
            {
                Vit = vit, MaxVit = maxVit, AddVit = addVit, BackVit = backVit, LastVitTime = lastVitTime, HasData = true,
            };
        }

        // ---- 伤害榜(46019/46022,recv 纯被动落表,非拉取 API) ----
        public DamageRankSelf DamageSelf { get; } = new DamageRankSelf();
        public readonly List<DamageRankEntry> DamageTop3 = new List<DamageRankEntry>();

        public void ApplyDamageRankSelf(int selfRank, long selfDamage, string selfName, long distance)
        {
            DamageSelf.SelfRank = selfRank; DamageSelf.SelfDamage = selfDamage;
            DamageSelf.SelfName = selfName ?? ""; DamageSelf.Distance = distance; DamageSelf.HasData = true;
        }

        public void ApplyDamageRankTop3(List<DamageRankEntry> list)
        {
            DamageTop3.Clear();
            if (list != null) DamageTop3.AddRange(list);
        }

        // ---- 免战(20201-205) ----
        public readonly List<WarFreeEntry> WarFreeList = new List<WarFreeEntry>();
        public bool HasWarFreeList { get; private set; }
        public long WarFreeEndTimeLeft { get; private set; } // 剩余秒数(对标老端 SetWarFreeTime)

        public void ApplyWarFreeList(List<WarFreeEntry> list)
        {
            WarFreeList.Clear();
            if (list != null) WarFreeList.AddRange(list);
            HasWarFreeList = true;
        }

        public void UpsertWarFree(int sceneType, int protectTime, int useCount)
        {
            for (int i = 0; i < WarFreeList.Count; i++)
            {
                if (WarFreeList[i].SceneType == sceneType)
                {
                    WarFreeList[i].ProtectTime = protectTime;
                    WarFreeList[i].UseCount = useCount;
                    return;
                }
            }
            WarFreeList.Add(new WarFreeEntry { SceneType = sceneType, ProtectTime = protectTime, UseCount = useCount });
        }

        /// <summary>20203 end_time 落地:按服务器时间戳算剩余(对标老端 `time = scmd.end_time - TimeUtil.getServerTime()`,
        /// end_time==0 则视为 0)。</summary>
        public void SetWarFreeEndTime(long endTime, long nowSec)
        {
            WarFreeEndTimeLeft = endTime == 0 ? 0 : Math.Max(0, endTime - nowSec);
        }

        public void Clear46000()
        {
            _boss.Clear();
            KillLog.Clear(); HasKillLog = false;
            DropLog.Clear(); HasDropLog = false;
            _vit.Clear();
            DamageSelf.HasData = false;
            DamageTop3.Clear();
            WarFreeList.Clear(); HasWarFreeList = false;
            WarFreeEndTimeLeft = 0;
        }
    }
}
