using System.Collections.Generic;
using Shenxiao.Common.Proto;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚姻(征友/戒指/结婚,自动循环 轮16)数据层:pt_172 172xx(征友17200-05/戒指17210-13/求婚·结婚·
    /// 离婚·秀恩爱17222-40/副本匹配邀请17245-97)+ 223xx 鲜花(22300-05)。33 号纯数据落地,不含表现层
    /// 逻辑(对标老端 MarriageModel.ts 的数据子集;UI View 消费方留尾包,见 MarriageController 类注释)。
    /// </summary>
    public sealed class MarriageModel
    {
        public static readonly MarriageModel Instance = new MarriageModel();
        private MarriageModel() { }

        // ============================================================================================
        // §1 征友 Personals(17200-17205)
        // ============================================================================================

        public sealed class TagEntry { public int TagId; public int TagSubId; }

        /// <summary>17200 player_list 单条(**无 CombatPower 字段**,勿与其它模块的角色行模板混淆)。</summary>
        public sealed class PersonalsEntry
        {
            public long RoleId;
            public string Name = "";
            public int Lv;
            public int Sex;
            public long Vip;
            public int Career;
            public int Turn;
            public int IfMarriage;
            public string Picture = "";
            public long PictureVer;
            public int IfOnline;
            public long Popularity;
            public string Msg = "";
            public int Type;
            public long Time;
            public int IfFollow;
            public int IfFriend;
            public long Intimacy;
            public readonly List<TagEntry> TagList = new List<TagEntry>();
            public long VipExp;
            public int VipHide;
            public int IsSupvip;
        }

        /// <summary>17200 单页数据(Page:1=大厅/2=我的关注/3=粉丝,按 page 分桶存,对标老端 _friend_dic_[page])。</summary>
        public sealed class PersonalsPage
        {
            public int Page;
            public long OwnPopularity;
            public long AskFollowTime;
            public long AskFlowerTime;
            public int LessFreeTimes;
            public readonly List<PersonalsEntry> PlayerList = new List<PersonalsEntry>();
        }

        private readonly Dictionary<int, PersonalsPage> _personalsPages = new Dictionary<int, PersonalsPage>();
        public IReadOnlyDictionary<int, PersonalsPage> PersonalsPages => _personalsPages;

        public void SetPersonalsPage(int page, PersonalsPage data) => _personalsPages[page] = data;
        public PersonalsPage GetPersonalsPage(int page) => _personalsPages.TryGetValue(page, out PersonalsPage v) ? v : null;

        /// <summary>17205 玩家细节(公会),无 Code 前缀独例,单条缓存最近一次查询结果。</summary>
        public sealed class RoleDetail { public long RoleId; public long GuildId; public string GuildName = ""; }
        public RoleDetail LastRoleDetail { get; private set; }
        public void SetRoleDetail(RoleDetail d) => LastRoleDetail = d;

        // ============================================================================================
        // §2 戒指 Ring(17210-17213)
        // ============================================================================================

        public sealed class PolishEntry { public long GoodsTypeId; public int UseNum; }
        public sealed class RingAttrEntry { public long AttrType; public long AttrNum; }

        public sealed class RingInfo
        {
            public int Stage;
            public int Star;
            public long PrayNum;
            /// <summary>服务端字段如实落地;老端用 config_ring_star 自算覆盖,本轮自算逻辑留 TODO
            /// (见 MarriageController 类注释),调用方暂读此服务端权威值。</summary>
            public long RingCombatPower;
            public readonly List<PolishEntry> PolishList = new List<PolishEntry>();
            public readonly List<RingAttrEntry> AttrList = new List<RingAttrEntry>();
        }

        public RingInfo Ring { get; private set; }
        public bool HasRing { get; private set; }

        /// <summary>17210 全量落地。</summary>
        public void SetRing(RingInfo info) { Ring = info; HasRing = true; }

        /// <summary>17211/17213 成功后原地更新阶/星/祈愿值(对标老端在既有 rinfo 上 mutate;若尚无 Ring
        /// 数据防御性新建,不强依赖调用顺序)。</summary>
        public void ApplyRingUpgrade(int stage, int star, long prayNum)
        {
            if (Ring == null) Ring = new RingInfo();
            Ring.Stage = stage;
            Ring.Star = star;
            Ring.PrayNum = prayNum;
            HasRing = true;
        }

        // ============================================================================================
        // §3 求婚/结婚/离婚/秀恩爱(17222-17240)
        // ============================================================================================

        public sealed class CostEntry { public long GoodsType; public long GoodsTypeId; public long GoodsNum; }

        /// <summary>17222 推送 / 17226 biaobai_list 单条共用形态(CombatPower 位宽独例由调用方各自号按 r16
        /// 读:17222=u32,17226=u64,本类字段用 long 兼容两者)。</summary>
        public sealed class ProposeEntry
        {
            public long RoleId;
            public string Name = "";
            public int Lv;
            public long CombatPower;
            public int Sex;
            public long Vip;
            public int Career;
            public int Turn;
            public string Picture = "";
            public long PictureVer;
            public int Type;
            public int ProposeType;
            public string Msg = "";
            public int IfAa;
            public readonly List<CostEntry> CostList = new List<CostEntry>();
        }

        /// <summary>17222 最近一条推送(求婚/再婚/离婚协商/礼包邀请)。</summary>
        public ProposeEntry LastPropose { get; private set; }
        public void SetLastPropose(ProposeEntry e) => LastPropose = e;

        /// <summary>17226 biaobai_answer_list 单条形态。</summary>
        public sealed class BiaobaiAnswerEntry
        {
            public long RoleId;
            public string Name = "";
            public int Lv;
            public long CombatPower;
            public int Sex;
            public long Vip;
            public int Career;
            public int Turn;
            public int Type;
            public int AnswerType;
        }

        public readonly List<ProposeEntry> BiaobaiList = new List<ProposeEntry>();
        public readonly List<BiaobaiAnswerEntry> BiaobaiAnswerList = new List<BiaobaiAnswerEntry>();
        public bool HasBiaobai { get; private set; }

        /// <summary>17226 全量落地(两个数组都落地;老端只读 biaobai_list、不读 answer_list,本端两者都存
        /// 供以后消费,比老端完整无害)。</summary>
        public void ApplyBiaobai(List<ProposeEntry> list, List<BiaobaiAnswerEntry> answerList)
        {
            BiaobaiList.Clear();
            if (list != null) BiaobaiList.AddRange(list);
            BiaobaiAnswerList.Clear();
            if (answerList != null) BiaobaiAnswerList.AddRange(answerList);
            HasBiaobai = true;
        }

        /// <summary>17224 回应结果推送(仅 AnswerType==1 时落地,==2 拒绝老端无任何分支,本端镜像不落地)。</summary>
        public sealed class AnswerResult { public long RoleId; public int Type; public int AnswerType; }
        public AnswerResult LastAnswerResult { get; private set; }
        public void SetLastAnswerResult(AnswerResult r) => LastAnswerResult = r;

        /// <summary>17229 键值(Key==1 对应恩爱值 LoveNum,其余原样透出供以后扩展消费)。</summary>
        private readonly Dictionary<int, long> _keyValues = new Dictionary<int, long>();
        public IReadOnlyDictionary<int, long> KeyValues => _keyValues;
        public void SetKeyValue(int key, long val) => _keyValues[key] = val;
        public long GetKeyValue(int key) => _keyValues.TryGetValue(key, out long v) ? v : 0;

        /// <summary>17232 我的伴侣(Figure 复用 FigureProto,含 is_marriage/marriage_id/marriage_name)。</summary>
        public sealed class MateInfo
        {
            public long RoleId;
            public long CombatPower;
            public FigureProto Figure;
            public int Type;
            public int NowWeddingState;
            public long AnniversaryTime;
            public long LoveNum;
            public int FirstMarriage;
        }

        public MateInfo Mate { get; private set; }
        public bool HasMate { get; private set; }

        /// <summary>17232 落地——code∈{1,1720012单身,1012}三码均调用(对标老端有意逻辑,由调用方判定后传入)。</summary>
        public void SetMate(MateInfo m) { Mate = m; HasMate = true; }

        /// <summary>17236 恩爱称号领取成功后的最近 id。</summary>
        public int LastDsgtId { get; private set; }
        public void SetLastDsgt(int id) => LastDsgtId = id;

        /// <summary>17238 真爱礼包信息。</summary>
        public sealed class GiftStateEntry { public int CountType; public int State; public long Time; }
        public sealed class GiftInfo
        {
            public long LoveGiftTimeS;
            public long LoveGiftTimeO;
            public readonly List<GiftStateEntry> GiftState = new List<GiftStateEntry>();
        }

        public GiftInfo Gift { get; private set; }
        public bool HasGift { get; private set; }
        public void SetGift(GiftInfo g) { Gift = g; HasGift = g != null; }

        /// <summary>17239 reward(ObjectList:u16计数{Type:8,TypeId:32,Num:32})。</summary>
        public sealed class RewardEntry { public int Type; public long TypeId; public long Num; }
        public int LastGiftRewardCountType { get; private set; }
        public readonly List<RewardEntry> LastGiftReward = new List<RewardEntry>();

        public void SetGiftReward(int countType, List<RewardEntry> list)
        {
            LastGiftRewardCountType = countType;
            LastGiftReward.Clear();
            if (list != null) LastGiftReward.AddRange(list);
        }

        // ============================================================================================
        // §4 副本匹配/邀请(17245-17297)——死链 UI(MarriageMatchView 系未定义类),数据层仍照接。
        // ============================================================================================

        public bool IsMatching { get; private set; }
        public int MatchDunId { get; private set; }
        public void SetMatchState(bool matching, int dunId) { IsMatching = matching; MatchDunId = dunId; }

        public sealed class MatchResultEntry { public int Type; public long RoleId; public FigureProto Figure; public long Power; }
        public sealed class MatchResult
        {
            public readonly List<MatchResultEntry> List = new List<MatchResultEntry>();
            public int EnterTime;
        }

        public MatchResult LastMatchResult { get; private set; }
        public void SetMatchResult(MatchResult r) => LastMatchResult = r;

        /// <summary>17296 收到的副本次数购买邀请。</summary>
        public sealed class DunInvite { public long RoleId; public string RoleName = ""; public int DunId; }
        public DunInvite LastDunInvite { get; private set; }
        public void SetLastDunInvite(DunInvite d) => LastDunInvite = d;

        // ============================================================================================
        // §5 鲜花(22300-22305)
        // ============================================================================================

        public sealed class FlowerInfo { public long FlowerNum; public long Charm; public long Fame; }
        public FlowerInfo Flower { get; private set; }
        public bool HasFlowerInfo { get; private set; }
        public void SetFlowerInfo(FlowerInfo f) { Flower = f; HasFlowerInfo = true; }

        /// <summary>22302 收礼记录(一次性全量,无分页,如实全量落地)。</summary>
        public sealed class FlowerRecordEntry
        {
            public long Id;
            public long SenderId;
            public string SenderName = "";
            public int ServerId;
            public int ServerNum;
            public long GoodsId;
            public int GoodsNum;
            public int Anonymous;
            public int IsThanks;
            public long Time;
        }

        public readonly List<FlowerRecordEntry> FlowerRecords = new List<FlowerRecordEntry>();
        public bool HasFlowerRecords { get; private set; }

        public void ApplyFlowerRecords(List<FlowerRecordEntry> list)
        {
            FlowerRecords.Clear();
            if (list != null) FlowerRecords.AddRange(list);
            HasFlowerRecords = true;
        }

        /// <summary>22304 收到的鲜花通知。</summary>
        public sealed class FlowerReceived
        {
            public long SenderId;
            public FigureProto SenderFigure;
            public int ServerId;
            public int ServerNum;
            public long GoodsId;
            public int GoodsNum;
        }

        public FlowerReceived LastFlowerReceived { get; private set; }
        public void SetLastFlowerReceived(FlowerReceived f) => LastFlowerReceived = f;

        // ============================================================================================

        public void Clear()
        {
            _personalsPages.Clear();
            LastRoleDetail = null;

            Ring = null; HasRing = false;

            LastPropose = null;
            BiaobaiList.Clear(); BiaobaiAnswerList.Clear(); HasBiaobai = false;
            LastAnswerResult = null;
            _keyValues.Clear();
            Mate = null; HasMate = false;
            LastDsgtId = 0;
            Gift = null; HasGift = false;
            LastGiftRewardCountType = 0; LastGiftReward.Clear();

            IsMatching = false; MatchDunId = 0;
            LastMatchResult = null;
            LastDunInvite = null;

            Flower = null; HasFlowerInfo = false;
            FlowerRecords.Clear(); HasFlowerRecords = false;
            LastFlowerReceived = null;
        }
    }
}
