using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Proto;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// 公会核心一期数据层(自动循环 轮13a;对标老端 commonModel/GuildModel.ts 第1组切面——基础/成员/
    /// 申请/职位/改名/合并/技能/声望/捐献推送/仙宗召援。与既有 <see cref="GuildJoinModel"/>(结社列表/建社)
    /// 并存,互不重叠——后者仍是"未入会"态列表数据,本类是"已入会"态公会自身数据。
    ///
    /// 权限判定<see cref="HasPermission"/>**修正**老端 `permission_list_.indexOf(id)` 的 truthy 判断 bug
    /// (JS indexOf 未命中返回 -1(truthy)、命中且在下标0返回0(falsy)——按此字面复刻会让"会长"因权限id=1排第0位
    /// 而永远查不到该权限,是明显的老端实现缺陷,不是设计意图;这里改用 Contains 保证行为正确,偏差记入 summary)。
    /// </summary>
    public sealed class GuildModel
    {
        public static readonly GuildModel Instance = new GuildModel();
        private GuildModel() { }

        // ---- 权限枚举(对标老端 GuildPermission,1-12) ----
        public static class Permission
        {
            public const int APPROVE_APPLY = 1;
            public const int APPOINT_POS = 2;
            public const int FIRE_MEMBER = 3;
            public const int MODIFY_TENET_AND_ANNOUNCE = 4;
            public const int APPROVE_SETTING = 5;
            public const int OPEN_ACT = 6;
            public const int JOIN_ACT = 7;
            public const int GUILD_LEVEL_UP = 8;
            public const int WAREHOUSE_MANAGER = 9;
            public const int STUDY_SKILL = 10;
            public const int SEND_GUILD_MAIL = 11;
            public const int POSITION_SETTING = 12;
        }

        /// <summary>对标老端 GuildOrder=[会长1,副会长2,精英5,宝贝4,会员3] 的"显示序"意图,按 position id 取序号。
        /// **老端字面行为比这更乱**:GuildModel.ts:163 `GuildOrder=[1,2,5,4,3]`(5元素,下标0-4),
        /// GuildController.ts:423 用 `GuildOrder[a.position]` 当映射表取值(不是当"目标序号数组"用)——
        /// pos1→2, pos2→5, pos3→4, pos4→3, pos5(下标越界)→undefined→NaN。也就是说老端可见排序其实是
        /// "会长 &lt; 宝贝 &lt; 会员 &lt; 副会长"(精英因 NaN 排序不稳定),并不只是 position==5 越界这一处小问题。
        /// 这里选择实现其"会长/副会长/精英/宝贝/会员"文档化意图序,不字面复刻老端这处乱序 bug
        /// (若后续要逐字节对标老端可见行为需改用 buggy 映射,不建议)。</summary>
        private static readonly Dictionary<int, int> POSITION_ORDER =
            new Dictionary<int, int> { { 1, 0 }, { 2, 1 }, { 5, 2 }, { 4, 3 }, { 3, 4 } };

        // ==================== 40005 基础信息 ====================

        public sealed class PositionEntry
        {
            public int Position;
            public long RoleId;
            public FigureProto Figure;
            public string Name => Figure?.name ?? "";
        }

        public sealed class GuildInfo
        {
            public long GuildId;
            public string GuildName = "";
            public string Announce = "";
            public readonly List<PositionEntry> PositionList = new List<PositionEntry>();
            public int GuildLv;
            public long Gfunds;
            public long GrowthVal;
            public long Gactivity;
            public int MemberNum;
            public int MemberCapacity;
            /// <summary>实为前十名成员战力之和(combat_power_ten),非全员战力总和。</summary>
            public long CombatPower;
            public int OnlineNum;
            public long DisbandWarnningTime;
            public int SalaryStatus;
            public int Division;
            public long JoinTime;
            public int IsInMerge;
        }

        public GuildInfo Info { get; private set; }
        public bool HasInfo => Info != null;

        public void SetInfo(GuildInfo info)
        {
            Info = info;
        }

        public void SetActivity(long gactivity)
        {
            if (Info != null) Info.Gactivity = gactivity;
        }

        public void SetFunds(long gfunds)
        {
            if (Info != null) Info.Gfunds = gfunds;
        }

        /// <summary>对标老端 GetTopMemberVo(pos):在 40005 自带的 position_list(非 40006 成员表)里找职位条目
        /// (会长=1);主界面显示会长名不需要等 40006 到齐。</summary>
        public PositionEntry GetTopMember(int position)
        {
            if (Info == null) return null;
            foreach (PositionEntry p in Info.PositionList) if (p.Position == position) return p;
            return null;
        }

        // ==================== 40006 成员列表 ====================

        public sealed class MemberEntry
        {
            public long RoleId;
            public FigureProto Figure;
            public int Position;
            public int TitleId;
            public long CombatPower;
            public bool Online;
            public long OfflineTime;
            public long CreateTime;
            public string Name => Figure?.name ?? "";
            public int Level => Figure?.level ?? 0;
            public int Career => Figure?.career ?? 0;
            public int Turn => Figure?.turn ?? 0;
        }

        private readonly List<MemberEntry> _members = new List<MemberEntry>();
        public IReadOnlyList<MemberEntry> Members => _members;
        public bool HasMembers { get; private set; }

        /// <summary>40006 全量落地 + 本地排序(对标老端 on40006:自己置顶→在线优先→职位序→等级)。</summary>
        public void SetMembers(List<MemberEntry> list, long selfRoleId)
        {
            _members.Clear();
            if (list != null) _members.AddRange(list);
            _members.Sort((a, b) =>
            {
                if (a.RoleId == selfRoleId && b.RoleId != selfRoleId) return -1;
                if (b.RoleId == selfRoleId && a.RoleId != selfRoleId) return 1;
                if (a.Online != b.Online) return a.Online ? -1 : 1;
                int ra = POSITION_ORDER.TryGetValue(a.Position, out int rax) ? rax : 99;
                int rb = POSITION_ORDER.TryGetValue(b.Position, out int rbx) ? rbx : 99;
                if (ra != rb) return ra - rb;
                return a.Level - b.Level;
            });
            HasMembers = true;
        }

        // ==================== 40008/40009/40016 申请列表 ====================

        public sealed class ApplyEntry
        {
            public long RoleId;
            public FigureProto Figure;
            public long CombatPower;
            public string Name => Figure?.name ?? "";
            public int Level => Figure?.level ?? 0;
        }

        private readonly List<ApplyEntry> _applies = new List<ApplyEntry>();
        public IReadOnlyList<ApplyEntry> Applies => _applies;
        public bool HasApplies { get; private set; }

        /// <summary>对标老端 GuildModel.apply_request_mark:点"查看申请"发 40008 后置位,回包到达时若非空
        /// 自动弹层(5秒内重复点击的去抖计时是 View 实例字段,不在这里——对标老端 apply_click_time 同样挂在
        /// View 而非 Model)。</summary>
        public bool ApplyRequestMark;

        public void SetApplies(List<ApplyEntry> list)
        {
            _applies.Clear();
            if (list != null) _applies.AddRange(list);
            HasApplies = true;
        }

        /// <summary>40009 审批成功后移除单条(**订正老端 splice(i,2) 过删 bug,rule10——服务端 40009 前必先
        /// 权威重推 40008,本条纠正对齐服务端"全量重推"设计意图,只删匹配的这一条**)。</summary>
        public void RemoveApply(long roleId)
        {
            for (int i = 0; i < _applies.Count; i++)
            {
                if (_applies[i].RoleId == roleId) { _applies.RemoveAt(i); return; }
            }
        }

        public void ClearApplies() => _applies.Clear();

        // ==================== 40021 权限列表 ====================

        private readonly HashSet<int> _permissions = new HashSet<int>();
        public bool HasPermissionInfo { get; private set; }

        public void SetPermissions(List<int> list)
        {
            _permissions.Clear();
            if (list != null) foreach (int p in list) _permissions.Add(p);
            HasPermissionInfo = true;
        }

        /// <summary>见类注释:修正老端 indexOf truthy 判断 bug,这里是正确的"是否持有该权限"判断。</summary>
        public bool HasPermission(int permissionType) => _permissions.Contains(permissionType);

        // ==================== 40010/40011 审批设置 ====================

        public int ApproveType { get; private set; }
        public int AutoApproveLv { get; private set; }
        public long AutoApprovePower { get; private set; }
        public bool HasApproveSetting { get; private set; }

        public void SetApproveSetting(int approveType, int autoApproveLv, long autoApprovePower)
        {
            ApproveType = approveType;
            AutoApproveLv = autoApproveLv;
            AutoApprovePower = autoApprovePower;
            HasApproveSetting = true;
        }

        // ==================== 40023 捐献信息(数据层保留,UI 不建) ====================

        public sealed class SelfGift { public int GiftId; public int GiftStatus; }
        public sealed class DonateRecord
        {
            public int DonateId;
            public long RoleId;
            public string RoleName;
            public int DonateType;
            public int Times;
            public int DonateAdd;
            public int GfundsAdd;
            public int GuildActivity;
            public long Time;
        }

        public int DonateTimes { get; private set; }
        public readonly List<SelfGift> SelfGifts = new List<SelfGift>();
        public readonly List<DonateRecord> DonateRecords = new List<DonateRecord>();
        public bool HasDonateInfo { get; private set; }

        public void SetDonateInfo(int donateTimes, List<SelfGift> selfGifts, List<DonateRecord> records)
        {
            DonateTimes = donateTimes;
            SelfGifts.Clear();
            if (selfGifts != null) SelfGifts.AddRange(selfGifts);
            DonateRecords.Clear();
            if (records != null) DonateRecords.AddRange(records);
            HasDonateInfo = true;
        }

        // ==================== 40039/40040/40042 贡献值 + 技能 ====================

        public int Donate { get; private set; }
        public void SetDonate(int donate) => Donate = donate;

        public sealed class SkillEntry
        {
            public int SkillId;
            public int LearnLv;
            public int ResearchLv;
            public long CurPower;
            public long NextPower;
        }

        public readonly List<SkillEntry> Skills = new List<SkillEntry>();
        public bool HasSkillInfo { get; private set; }

        public void SetSkills(List<SkillEntry> list)
        {
            Skills.Clear();
            if (list != null) Skills.AddRange(list);
            HasSkillInfo = true;
        }

        /// <summary>40042 学习成功原地 patch(对标老端 on40042)。</summary>
        public void PatchSkill(int skillId, int learnLv, long curPower, long nextPower)
        {
            foreach (SkillEntry s in Skills)
            {
                if (s.SkillId != skillId) continue;
                s.LearnLv = learnLv;
                s.CurPower = curPower;
                s.NextPower = nextPower;
                return;
            }
        }

        // ==================== 40030/40031 声望 ====================

        public int AllPrestige { get; private set; }
        public int TitleId { get; private set; }
        public int PrestigeWeek { get; private set; }
        public int PrestigeLimit { get; private set; }
        public bool HasPrestige { get; private set; }

        public void SetPrestige(int allPrestige, int titleId, int prestigeWeek, int prestigeLimit)
        {
            AllPrestige = allPrestige;
            TitleId = titleId;
            PrestigeWeek = prestigeWeek;
            PrestigeLimit = prestigeLimit;
            HasPrestige = true;
        }

        public int PrestigeDay { get; private set; }
        public int PrestigeDayLimit { get; private set; }

        public void SetPrestigeDaily(int allPrestige, int prestigeDay, int prestigeDayLimit)
        {
            AllPrestige = allPrestige;
            PrestigeDay = prestigeDay;
            PrestigeDayLimit = prestigeDayLimit;
        }

        // ==================== 40044 改名信息 ====================

        public bool RenameIsFree { get; private set; }
        /// <summary>**语义:剩余秒数倒计时,非墙钟时间戳**(mod_guild_cast.erl:864-871
        /// `NextRenameTime = LastRenameTime + RenameInterval - NowTime`(≤0 则 0),即服务端已经算好的净剩余秒数。
        /// 消费方倒计时要用 TimeUtil 本地推(now+这个值起算),不要再拿服务器墙钟相减。</summary>
        public long NextRenameTime { get; private set; }
        public bool HasRenameInfo { get; private set; }

        public void SetRenameInfo(bool isFree, long nextRenameTime)
        {
            RenameIsFree = isFree;
            NextRenameTime = nextRenameTime;
            HasRenameInfo = true;
        }

        // ==================== 40060 仙宗召援 ====================

        public sealed class BossCallInfo
        {
            public long RoleId;
            public string RoleName;
            public int RoleLv;
            public int RoleCareer;
            public int RoleSex;
            public string RolePic;
            public long RolePicVer;
            public int BossType;
            public string BossTypeName;
            public int BossId;
            public int Layer;
            public int SceneId;
            public int X;
            public int Y;
        }

        public BossCallInfo LastBossCall { get; private set; }
        /// <summary>本次召援是否由自己发起(对标老端 boss_mark,自己发起的不弹自己的提示)。</summary>
        public bool BossCallSelfMark;

        public void SetLastBossCall(BossCallInfo info) => LastBossCall = info;

        // ==================== 40061 合并候选(item_to_bin_12,与 40001 item_to_bin_0 同结构) ====================

        public sealed class MergeCandidate
        {
            public long GuildId;
            public string GuildName;
            public int GuildLv;
            public long Gfunds;
            public long ChiefId;
            public string ChiefName;
            public int MemberNum;
            public int MemberCapacity;
            public bool IsApply;
            public long AutoApprovePower;
            public long CombatPower;
            public int MergeStatus;
            /// <summary>三态(0无关/1主/2副),非布尔——对标 lib_guild_util:calc_guild_merge_rel。</summary>
            public int MergeRel;
        }

        public readonly List<MergeCandidate> MergeCandidates = new List<MergeCandidate>();
        public bool HasMergeCandidates { get; private set; }

        public void SetMergeCandidates(List<MergeCandidate> list)
        {
            MergeCandidates.Clear();
            if (list != null) MergeCandidates.AddRange(list);
            HasMergeCandidates = true;
        }

        // ==================== 场景广播(40017,数据不落本模型,仅供未来场景消费方参考) ====================

        public sealed class SceneGuildTag
        {
            public long RoleId;
            public long GuildId;
            public string GuildName;
            public int Position;
            public string PositionName;
        }

        // ==================== 通用变长奖励(ObjectList,对标服务端 pt:write_object_list:u16 计数+{style:c,type_id:i,num:i}) ====================

        public struct RewardEntry
        {
            public int Style;
            public int TypeId;
            public long Num;
        }

        internal static List<RewardEntry> ReadRewardList(Shenxiao.Framework.Net.NetReader r)
            => r.ReadArray(rr => new RewardEntry { Style = rr.ReadU8(), TypeId = (int)rr.ReadU32(), Num = rr.ReadU32() });

        // ==================== 公会二期(轮13b):结社仓库(40100-110,pt_401) ====================

        /// <summary>仓库物品条目(对标服务端 pt_401.erl item_to_bin_5/_10,13 字段;嵌套宝石/洗炼/极品/附加属性
        /// 数组按序读过不留——本轮 UI 仅需 TypeId/Num/Color 铺 EquipmentItem 格子,实例属性显示待后续 tips 移植)。</summary>
        public sealed class DepotGoodsEntry
        {
            public long GoodsId;
            public int TypeId;
            public long Num;
            public int Color;
            public long Rating;
            public long OverallRating;
            public int SuitLv;
            public int SuitSlv;
            public int SuitCount;
        }

        /// <summary>兑换记录条目(对标 item_to_bin_0/_16,16 字段;比 DepotGoodsEntry 多 RecordId/RoleName/
        /// ExchangeType/Time 四项来源信息,无 Num——单条记录代表一次操作)。</summary>
        public sealed class DepotRecordEntry
        {
            public int RecordId;
            public string RoleName;
            public int ExchangeType;
            public long GoodsId;
            public int TypeId;
            public int Color;
            public long Rating;
            public long OverallRating;
            public int SuitLv;
            public int SuitSlv;
            public int SuitCount;
            public long Time;
        }

        /// <summary>仓库任务装备虚构占位条目 GoodsId(对标服务端 `?GUILD_DEPOT_TASK_EQUIP`=1;40103 兑换该id时
        /// Num 必须精确=1,否则被服务端错误路由到通用兑换分支;40106 增量里该id配 num=0 代表"虚构条目清零",
        /// 不是真实仓库物品变化)。</summary>
        public const long DEPOT_TASK_EQUIP_GOODS_ID = 1;

        public int DepotScore { get; private set; }
        private readonly List<DepotGoodsEntry> _depotGoods = new List<DepotGoodsEntry>();
        private readonly List<DepotRecordEntry> _depotRecords = new List<DepotRecordEntry>();
        public IReadOnlyList<DepotGoodsEntry> DepotGoods => _depotGoods;
        public IReadOnlyList<DepotRecordEntry> DepotRecords => _depotRecords;
        public bool HasDepotInfo { get; private set; }
        /// <summary>当前生效的自动清理条件(40109 设置后由 40110 查询回显;stage=0 代表未生效)。</summary>
        public int AutoDestroyStage { get; private set; }
        public int AutoDestroyColor { get; private set; }
        public int AutoDestroyStar { get; private set; }

        /// <summary>40101 全量落地(清空重建,对标老端仓库主体一次性下发)。</summary>
        public void SetDepotInfo(int depotScore, List<DepotRecordEntry> records, List<DepotGoodsEntry> goods)
        {
            DepotScore = depotScore;
            _depotRecords.Clear();
            if (records != null) _depotRecords.AddRange(records);
            _depotGoods.Clear();
            if (goods != null) _depotGoods.AddRange(goods);
            HasDepotInfo = true;
        }

        public void SetDepotScore(int score) => DepotScore = score;

        /// <summary>40105 新增推送(逐条插入,对标老端)。**Guard**:底表(40101)未加载则忽略——对标老端
        /// on40105 `depot_info` 判空 return,避免推送先于全量到达时凭空建表。</summary>
        public void AddDepotGoods(List<DepotGoodsEntry> list)
        {
            if (!HasDepotInfo || list == null) return;
            _depotGoods.AddRange(list);
        }

        /// <summary>40106 数量增量(按 GoodsId 更新已有条目;num&lt;=0 物理移除)。**Guard**:底表未加载则忽略;
        /// **对标老端"只更新已有条目从不新增"**——未知 goods_id 直接丢弃,不再伪造 TypeId=0 幽灵条目
        /// (该 id 必然已由 40101/40105 下发过,收到未知 id 只可能是时序错乱)。</summary>
        public void ApplyDepotGoodsNum(List<(long goodsId, long num)> deltas)
        {
            if (!HasDepotInfo || deltas == null) return;
            foreach ((long goodsId, long num) d in deltas)
            {
                int idx = _depotGoods.FindIndex(g => g.GoodsId == d.goodsId);
                if (idx < 0) continue;
                if (d.num <= 0) _depotGoods.RemoveAt(idx);
                else _depotGoods[idx].Num = d.num;
            }
        }

        /// <summary>40107 兑换记录头插(对标老端"头插本地记录列表")。**Guard**:底表未加载则忽略。</summary>
        public void PrependDepotRecords(List<DepotRecordEntry> list)
        {
            if (!HasDepotInfo || list == null || list.Count == 0) return;
            _depotRecords.InsertRange(0, list);
        }

        public void SetAutoDestroySetting(int stage, int color, int star)
        {
            AutoDestroyStage = stage;
            AutoDestroyColor = color;
            AutoDestroyStar = star;
        }

        // ==================== 公会二期(轮13b):结社宝箱(40300-305,pt_403) ====================

        public sealed class BoxSendEntry
        {
            public long AutoId;
            public string RoleName;
            public long RoleId;
            public int TaskId;
            public int Status;
            public List<RewardEntry> Reward;
            public long Time;
        }

        public sealed class BoxLogEntry
        {
            public string RoleName;
            public long RoleId;
            public int TaskId;
            public long Time;
        }

        public sealed class BoxTaskInfo
        {
            public int TaskId;
            public int SendNum;
        }

        public int BoxNum { get; private set; }
        public int BoxMaxNum { get; private set; }
        private readonly List<BoxSendEntry> _boxSendList = new List<BoxSendEntry>();
        private readonly List<BoxLogEntry> _boxLog = new List<BoxLogEntry>();
        private readonly Dictionary<int, int> _boxTaskInfo = new Dictionary<int, int>(); // taskId -> sendNum
        public IReadOnlyList<BoxSendEntry> BoxSendList => _boxSendList;
        public IReadOnlyList<BoxLogEntry> BoxLog => _boxLog;
        public bool HasBoxInfo { get; private set; }

        public int GetBoxTaskSendNum(int taskId) => _boxTaskInfo.TryGetValue(taskId, out int n) ? n : 0;

        /// <summary>40301 全量落地。</summary>
        public void SetBoxInfo(int num, int maxNum, List<BoxSendEntry> sendList, List<BoxLogEntry> log, List<BoxTaskInfo> info)
        {
            BoxNum = num;
            BoxMaxNum = maxNum;
            _boxSendList.Clear();
            if (sendList != null) _boxSendList.AddRange(sendList);
            _boxLog.Clear();
            if (log != null) _boxLog.AddRange(log);
            // 必须先置 HasBoxInfo 再 ApplyBoxTaskInfo——后者带"底表未加载则忽略"防御门,
            // 原顺序会把首个 40301 自带的任务次数表挡在门外(轮13b 批处理实跑抓出)。
            HasBoxInfo = true;
            ApplyBoxTaskInfo(info);
        }

        /// <summary>40303 增量新增(对标老端 updateRewardBox:send/log 均 unshift 头插——日志流"最新在前",
        /// 与 <see cref="PrependDepotRecords"/> 同款,不能用尾插 AddRange)。**Guard**:底表(40301)未加载则忽略
        /// (对标老端 `_rewardBoxViewData` 判空 return)。</summary>
        public void AddBoxEntries(List<BoxSendEntry> sendList, List<BoxLogEntry> log)
        {
            if (!HasBoxInfo) return;
            if (sendList != null) _boxSendList.InsertRange(0, sendList);
            if (log != null) _boxLog.InsertRange(0, log);
        }

        /// <summary>40304 按 id 移除单条(公会全员广播,过期/GM清空)。**Guard**:底表未加载则忽略。</summary>
        public void RemoveBoxEntry(long autoId)
        {
            if (!HasBoxInfo) return;
            _boxSendList.RemoveAll(e => e.AutoId == autoId);
        }

        /// <summary>40302 领取成功后原地移除已领条目(对标老端"补发40301刷新",本地先行摘除避免重复展示)。</summary>
        public void RemoveBoxEntries(IEnumerable<long> autoIds)
        {
            if (autoIds == null) return;
            var set = new HashSet<long>(autoIds);
            _boxSendList.RemoveAll(e => set.Contains(e.AutoId));
        }

        /// <summary>40305:**按条 upsert 而非全量替换**——同一协议号在"单人完成任务"(增量1条)与
        /// "day_clear/gm_clear 全服广播"(全量所有任务id)两种触发源下语义不同,upsert 天然兼容两者,
        /// 且不假设收到即代表自己有公会(纯按 TaskId 更新,不触发任何"无公会"相关逻辑)。**Guard**:底表未加载
        /// 则忽略(对标老端 `_rewardBoxViewData` 判空 return——与"是否有公会"无关,是"宝箱视图是否加载过")。</summary>
        public void ApplyBoxTaskInfo(List<BoxTaskInfo> info)
        {
            if (!HasBoxInfo || info == null) return;
            foreach (BoxTaskInfo i in info) _boxTaskInfo[i.TaskId] = i.SendNum;
        }

        // ==================== 公会二期(轮13b):结社协助(40401-410,pt_404) ====================

        public sealed class AssistExtra
        {
            public int SerId;
            public int SerNum;
            public long RoberId;
            public string RoberName;
            public long RoberPower;
            public List<RewardEntry> RoberReward;
            public List<RewardEntry> BackReward;
        }

        /// <summary>求助条目(对标 pt_404.erl item_to_bin_0,14 字段;40405 列表项/40406 新求助推送共用同一结构)。</summary>
        public sealed class AssistEntry
        {
            public long AssistId;
            public int Type;
            public int SubType;
            public int TargetCfgId;
            public long TargetId;
            public long RoleId;
            public string Name;
            public int Level;
            public int Career;
            public int Sex;
            public string Pic;
            public long PicVer;
            public bool IsAssist;
            public List<AssistExtra> Extra;
        }

        /// <summary>当前正在协助的对象(40408,12 字段——比 AssistEntry 少 IsAssist/Extra)。</summary>
        public sealed class MyAssistInfo
        {
            public long AssistId;
            public int Type;
            public int SubType;
            public int TargetCfgId;
            public long TargetId;
            public long RoleId;
            public string Name;
            public int Level;
            public int Career;
            public int Sex;
            public string Pic;
            public long PicVer;
        }

        /// <summary>我方主动发起、尚未被接受/取消的求助(40401 成功回显,对标老端 ReqData;
        /// On40403 isSelf 分支/On40407 assistId 命中时清空——与 <see cref="CurrentMyAssist"/>(对标老端
        /// AssistData/"我在协助谁")是两条独立状态:一个是"我发的求助",一个是"我在帮的人")。</summary>
        public sealed class MyAssistRequest
        {
            public long AssistId;
            public int Type;
            public int SubType;
            public int TargetCfgId;
            public long TargetId;
        }

        public int AssistCount { get; private set; }
        private readonly List<AssistEntry> _assistList = new List<AssistEntry>();
        public IReadOnlyList<AssistEntry> AssistList => _assistList;
        public bool HasAssistList { get; private set; }
        public MyAssistInfo CurrentMyAssist { get; private set; }
        public MyAssistRequest MyRequest { get; private set; }

        public void SetAssistCount(int count) => AssistCount = count;

        /// <summary>40405 全量落地(服务端无长度上限,不做客户端截断)。</summary>
        public void SetAssistList(List<AssistEntry> list)
        {
            _assistList.Clear();
            if (list != null) _assistList.AddRange(list);
            HasAssistList = true;
        }

        /// <summary>40406 新求助推送:同 AssistId 已存在则替换(理论不该发生),否则追加。**Guard**:底表(40405)
        /// 未加载则忽略(对标老端 on40406 `hdata` 判空才 table.insert)。</summary>
        public void UpsertAssist(AssistEntry entry)
        {
            if (!HasAssistList || entry == null) return;
            int idx = _assistList.FindIndex(a => a.AssistId == entry.AssistId);
            if (idx >= 0) _assistList[idx] = entry;
            else _assistList.Add(entry);
        }

        /// <summary>40407 按条移除(**扇出模式,一次只处理一条,不当全量刷新**)。</summary>
        public void RemoveAssist(long assistId)
        {
            _assistList.RemoveAll(a => a.AssistId == assistId);
        }

        public void SetMyAssist(MyAssistInfo info) => CurrentMyAssist = info;

        public void ClearMyAssist() => CurrentMyAssist = null;

        public void SetMyRequest(MyAssistRequest info) => MyRequest = info;

        public void ClearMyRequest() => MyRequest = null;

        /// <summary>对标老端 GuildModel.IsCanOpenAssist：已有公会，且同时满足 config_guild_constant
        /// 26(角色等级)和28(开服天数)。配置未就绪或值异常时按未开放处理，避免把 type=1/2 求助误显出来。</summary>
        public static bool IsAssistGloballyOpen()
        {
            if (!IsHasGuild()) return false;
            if (!int.TryParse(GuildConfigs.GetKv(26), out int requiredLevel)) return false;
            if (!int.TryParse(GuildConfigs.GetKv(28), out int requiredOpenDay)) return false;
            int level = Shenxiao.Module.Core.Role.RoleModel.Instance.HasBaseInfo
                ? Shenxiao.Module.Core.Role.RoleModel.Instance.Level : 0;
            int openDay = Shenxiao.Module.Core.Game.ServerTimeModel.GetOpenServerDay();
            return level >= requiredLevel && openDay >= requiredOpenDay;
        }

        /// <summary>对标老端 GuildModel.IsOpenAssist：按 config_guild_assist 的 role_lv/open_day 条件
        /// 判定单个协助类型是否开放。只接受当前配置已定义的条件；未知条件不在 Guild 岛猜测。</summary>
        public static bool IsAssistOpen(int type, int subType)
        {
            if (!IsHasGuild()) return false;
            JObject cfg = GuildConfigs.GetAssistCfg(type, subType);
            if (cfg == null) return false;
            JArray conditions = ParseAssistConditions(cfg["condition"]?.ToString());
            if (conditions == null) return false;

            int level = Shenxiao.Module.Core.Role.RoleModel.Instance.HasBaseInfo
                ? Shenxiao.Module.Core.Role.RoleModel.Instance.Level : 0;
            int openDay = Shenxiao.Module.Core.Game.ServerTimeModel.GetOpenServerDay();
            foreach (JToken row in conditions)
            {
                string key = row?["0"]?.ToString();
                if (!int.TryParse(row?["1"]?.ToString(), out int value)) return false;
                if (key == "role_lv" && level < value) return false;
                if (key == "open_day" && openDay < value) return false;
                if (key != "role_lv" && key != "open_day") return false;
            }
            return true;
        }

        /// <summary>对标老端 GetAssistOpenDesc，倒序拼为“开服第N天且M级”。</summary>
        public static string GetAssistOpenDescription(int type, int subType)
        {
            if (!IsHasGuild()) return "请先加入或创建一个公会~";
            JObject cfg = GuildConfigs.GetAssistCfg(type, subType);
            string functionName = cfg?["desc"]?.ToString() ?? "该功能";
            JArray conditions = ParseAssistConditions(cfg?["condition"]?.ToString());
            if (conditions == null) return functionName + "结社协助暂未开放~";

            var parts = new List<string>();
            for (int i = conditions.Count - 1; i >= 0; i--)
            {
                string key = conditions[i]?["0"]?.ToString();
                string value = conditions[i]?["1"]?.ToString();
                if (string.IsNullOrEmpty(value)) continue;
                if (key == "role_lv") parts.Add(value + "级");
                else if (key == "open_day") parts.Add("开服第" + value + "天");
            }
            return functionName + "结社协助将在" + string.Join("且", parts) + "时开启~";
        }

        private static JArray ParseAssistConditions(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            try { return JArray.Parse(raw); }
            catch (Exception) { return null; }
        }

        /// <summary>对标老端 GuildModel.IsMergeOpen：config_guild_constant 29 是合并功能开服日。</summary>
        public static bool IsMergeOpen()
        {
            return int.TryParse(GuildConfigs.GetKv(29), out int requiredOpenDay)
                && Shenxiao.Module.Core.Game.ServerTimeModel.GetOpenServerDay() >= requiredOpenDay;
        }

        /// <summary>对标老端 GuildIdolIsOpen：神像 KV 的 open_day/lv_limit 双门槛。</summary>
        public static bool IsGuildIdolOpen()
        {
            JObject openDayRow = GuildConfigs.GetGodKv("open_day");
            JObject levelRow = GuildConfigs.GetGodKv("lv_limit");
            if (!int.TryParse(openDayRow?["value"]?.ToString(), out int requiredOpenDay)) return false;
            if (!int.TryParse(levelRow?["value"]?.ToString(), out int requiredLevel)) return false;
            int level = Shenxiao.Module.Core.Role.RoleModel.Instance.HasBaseInfo
                ? Shenxiao.Module.Core.Role.RoleModel.Instance.Level : 0;
            return Shenxiao.Module.Core.Game.ServerTimeModel.GetOpenServerDay() >= requiredOpenDay
                && level >= requiredLevel;
        }

        // ==================== 公会二期(轮13b):结社武魂/神像(40500-509,pt_405;per-player 数据,独立分区
        //        ——存储层与 GuildId 无关,仅解锁门槛依赖公会等级/头衔,不做全公会广播) ====================

        public sealed class GodEntry
        {
            public int GodId;
            public int Color;
            public int Lv;
            public long GodPower;
        }

        public sealed class GodRuneEntry
        {
            public int Pos;
            public long GoodsId;
            public int GoodsTypeId;
        }

        /// <summary>单神像铭文详情(40502,GodId 对标本轮"万能刷新推送号")。</summary>
        public sealed class GodDetail
        {
            public int GodId;
            public readonly List<GodRuneEntry> RuneList = new List<GodRuneEntry>();
            public int ComboId;
            public readonly List<int> AchievementLvs = new List<int>();
            public long GodPower;
        }

        /// <summary>结社头衔等级(40501 GuildTitleLv,神像解锁门槛用——与 40030 声望的 TitleId 是两个不同概念,
        /// 不要混用)。</summary>
        public int GodGuildTitleLv { get; private set; }
        private readonly List<GodEntry> _godList = new List<GodEntry>();
        public IReadOnlyList<GodEntry> GodList => _godList;
        public bool HasGodInfo { get; private set; }
        private readonly Dictionary<int, GodDetail> _godDetails = new Dictionary<int, GodDetail>();

        public GodEntry GetGod(int godId) => _godList.Find(g => g.GodId == godId);
        public GodDetail GetGodDetail(int godId) => _godDetails.TryGetValue(godId, out GodDetail d) ? d : null;

        /// <summary>40501 全量落地(遍历配置里全部神像id,未激活以 {Id,0,0,0} 占位——非"已拥有"列表)。</summary>
        public void SetGodList(int guildTitleLv, List<GodEntry> list)
        {
            GodGuildTitleLv = guildTitleLv;
            _godList.Clear();
            if (list != null) _godList.AddRange(list);
            HasGodInfo = true;
        }

        /// <summary>40502 单神像详情落地(按 GodId 覆盖式 upsert)。</summary>
        public void SetGodDetail(GodDetail detail)
        {
            if (detail == null) return;
            _godDetails[detail.GodId] = detail;
        }

        /// <summary>40503/40504 升品/觉醒成功原地 patch GodList 对应条目(**同一字段位置语义在两号里不同**,
        /// 由调用方按号自行确定含义,这里只负责原样写入)。</summary>
        public void PatchGod(int godId, int color, int lv, long godPower)
        {
            GodEntry g = GetGod(godId);
            if (g == null) { _godList.Add(new GodEntry { GodId = godId, Color = color, Lv = lv, GodPower = godPower }); return; }
            g.Color = color;
            g.Lv = lv;
            g.GodPower = godPower;
        }

        // ==================== 重置(断线/登出/退会/解散) ====================

        public void Reset()
        {
            Info = null;
            _members.Clear();
            HasMembers = false;
            _applies.Clear();
            HasApplies = false;
            ApplyRequestMark = false;
            _permissions.Clear();
            HasPermissionInfo = false;
            HasApproveSetting = false;
            DonateTimes = 0;
            SelfGifts.Clear();
            DonateRecords.Clear();
            HasDonateInfo = false;
            Donate = 0;
            Skills.Clear();
            HasSkillInfo = false;
            AllPrestige = 0; TitleId = 0; PrestigeWeek = 0; PrestigeLimit = 0; HasPrestige = false;
            PrestigeDay = 0; PrestigeDayLimit = 0;
            RenameIsFree = false; NextRenameTime = 0; HasRenameInfo = false;
            LastBossCall = null; BossCallSelfMark = false;
            MergeCandidates.Clear();
            HasMergeCandidates = false;

            DepotScore = 0;
            _depotGoods.Clear();
            _depotRecords.Clear();
            HasDepotInfo = false;
            AutoDestroyStage = 0; AutoDestroyColor = 0; AutoDestroyStar = 0;

            BoxNum = 0; BoxMaxNum = 0;
            _boxSendList.Clear();
            _boxLog.Clear();
            _boxTaskInfo.Clear();
            HasBoxInfo = false;

            AssistCount = 0;
            _assistList.Clear();
            HasAssistList = false;
            CurrentMyAssist = null;
            MyRequest = null;

            GodGuildTitleLv = 0;
            _godList.Clear();
            HasGodInfo = false;
            _godDetails.Clear();
        }

        /// <summary>是否会长(对标老端 IsGuildMaster:mainRoleVo.position==1)。</summary>
        public static bool IsGuildMaster() => Shenxiao.Module.Core.Role.RoleModel.Instance.GuildPosition == 1;

        /// <summary>是否已有公会(对标老端 IsHasGuild:mainRoleVo.guild_id>0)。</summary>
        public static bool IsHasGuild() => Shenxiao.Module.Core.Role.RoleModel.Instance.GuildId > 0;

        private static readonly string[] LEGACY_ANNOUNCES =
        {
            "欢迎你加入结社！每日完成结社任务打大妖拿装备；多余装备捐仓库，积分换装备；与结社成员组团打大妖，保障又高效！",
            "欢迎你加入结社！每日完成结社任务打首领拿装备；多余装备捐仓库，积分换装备；与结社成员组团打首领，保障又高效！",
            "欢迎你加入结社！每日完成仙宗任务打大妖拿装备；多余装备捐仓库，积分换装备；与结社成员组团打大妖，保障又高效！",
            "欢迎你加入结社！每日完成仙宗任务打首领拿装备；多余装备捐仓库，积分换装备；与结社成员组团打首领，保障又高效！",
        };

        private const string DEFAULT_ANNOUNCE =
            "欢迎你拜入仙宗！每日完成宗门任务斩大妖取装备；多余装备献仙库，功德积分换神装；与同门结伴诛大妖，稳妥又高效！";

        /// <summary>对标老端 GuildModel.RemapGuildAnnounce:旧版"结社"默认公告文案映射为"仙宗"新文案。</summary>
        public static string RemapAnnounce(string text)
        {
            if (string.IsNullOrEmpty(text)) return DEFAULT_ANNOUNCE;
            string trimmed = text.Trim();
            if (trimmed.StartsWith("欢迎你加入结社")) return DEFAULT_ANNOUNCE;
            foreach (string legacy in LEGACY_ANNOUNCES)
            {
                string legacyTrimmed = legacy.Trim();
                if (trimmed == legacyTrimmed || trimmed.Contains(legacyTrimmed)) return DEFAULT_ANNOUNCE;
            }
            return text;
        }
    }
}
