using System.Collections.Generic;
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
