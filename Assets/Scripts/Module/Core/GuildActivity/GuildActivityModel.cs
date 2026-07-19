using System.Collections.Generic;

namespace Shenxiao.Module.Core.GuildActivity
{
    /// <summary>
    /// 公会晚宴(pt_402 主体,自动循环 轮22 PK1)数据层:公会BOSS(40201/03/04/08/09)+ 晚宴主流程
    /// (40211/12/14/17/20/21/22)+ 篝火/答题/龙魂/菜肴(40255/56/57/58/59/60/62/64/65/66/67)+
    /// 族错误出口 40200,共 26 号纯数据落地,不含表现层逻辑(对标老端 GuildActivityModel.ts 的数据子集;
    /// UI 33 个 view 里 prefab 只烤了 4 个,消费方留尾包,见 GuildActivityController 类注释)。
    /// 结社守卫(40230-32)按主控裁决2 全部 killlist,不在此模块范围内。
    /// </summary>
    public sealed class GuildActivityModel
    {
        public static readonly GuildActivityModel Instance = new GuildActivityModel();
        private GuildActivityModel() { }

        // ============================================================================================
        // §1 公会BOSS(40201 信息/40203 兽粮被动推送/40204 召集/40208 结算/40209 自动召唤)
        // ============================================================================================

        public sealed class BossInfo
        {
            public long Etime;
            public long AutoDrumupTime;
            public long DunId;
            public long GbossMat;
            public int RemainTimes;
            public int IsAuto;
            public int IsDrumToday;
            public int MonState;
        }

        public BossInfo Boss { get; private set; }
        public bool HasBoss { get; private set; }
        public void SetBoss(BossInfo b) { Boss = b; HasBoss = true; }

        /// <summary>40203 兽粮被动推送(内部触发,见 lib_gift_new.erl:541/lib_goods_api.erl:670,674,非 c2s)。
        /// 老端只弹提示"获得神兽诱饵:xx"不落地状态;本端原地刷新 Boss.GbossMat 到推送总量,比老端完整无害。</summary>
        public long LastGbossMatAdd { get; private set; }
        public void ApplyGbossMatAdd(long add, long total)
        {
            LastGbossMatAdd = add;
            if (Boss != null) Boss.GbossMat = total;
        }

        /// <summary>40204 召集成功后老端原地置 IsDrumToday=1(GuildActivityController.ts:279-282)。</summary>
        public void ApplyCallBossSuccess()
        {
            if (Boss != null) Boss.IsDrumToday = 1;
        }

        /// <summary>40209 自动召唤设置成功后原地更新 IsAuto(老端未显式回写,本端为数据一致性补做,无害)。</summary>
        public void ApplyAutoDrumSet(int isAuto)
        {
            if (Boss != null) Boss.IsAuto = isAuto;
        }

        /// <summary>40208 结算奖励条目:Gtype:8,GtypeId:32,Gnum:**16**(独例,勿与标准 ObjectList 的 Num:32 混淆,
        /// pt_402.erl item_to_bin_0/1)。</summary>
        public sealed class GbossRewardEntry
        {
            public int Type;
            public long TypeId;
            public int Num;
        }

        public sealed class BossResult
        {
            public int GbossResult;
            public readonly List<GbossRewardEntry> FixReward = new List<GbossRewardEntry>();
            public readonly List<GbossRewardEntry> AuctionReward = new List<GbossRewardEntry>();
        }

        public BossResult LastBossResult { get; private set; }
        public void SetBossResult(BossResult r) => LastBossResult = r;

        // ============================================================================================
        // §2 晚宴主流程(40211 核心驱动/40212 进场景/40214 积分榜/40217 答题信息/40220 个人排名/
        // 40221 小游戏完成/40222 当日小游戏类型)
        // ============================================================================================

        public sealed class ActInfo
        {
            public int Status;
            public long ActEndTime;
            public long Etime;
            public int Stage;
        }

        public ActInfo Act { get; private set; }
        public bool HasAct { get; private set; }
        public void SetAct(ActInfo a) { Act = a; HasAct = true; }

        public sealed class RankGuildEntry
        {
            public long GuildId;
            public long ServerNum;
            public string GuildName = "";
            public long GuildScore;
            public int GuildRank;
        }

        public sealed class RankServerEntry
        {
            public long SerId;
            public long SerNum;
            public int Rank;
            public string Name = "";
            public long Score;
        }

        public sealed class RankInfo
        {
            public int IsKf;
            public readonly List<RankGuildEntry> GuildList = new List<RankGuildEntry>();
            public readonly List<RankServerEntry> RankList = new List<RankServerEntry>();
        }

        public RankInfo Rank { get; private set; }
        public bool HasRank { get; private set; }
        public void SetRank(RankInfo r) { Rank = r; HasRank = true; }

        public sealed class QuestInfo
        {
            public int Status;
            public long Etime;
            public long No;
            public long Id;
        }

        public QuestInfo Quest { get; private set; }
        public void SetQuest(QuestInfo q) => Quest = q;

        public int MyRank { get; private set; }
        public long MyPoint { get; private set; }
        public bool HasMyRank { get; private set; }
        public void SetMyRank(int rank, long point) { MyRank = rank; MyPoint = point; HasMyRank = true; }

        public bool MiniGameFinished { get; private set; }
        public bool HasMiniGameStatus { get; private set; }
        public void SetMiniGameFinished(int isFinish) { MiniGameFinished = isFinish != 0; HasMiniGameStatus = true; }

        /// <summary>当天轮换的小游戏类型:1=答题/2=消消乐(对标老端 evening_game_type)。</summary>
        public int GameType { get; private set; }
        public void SetGameType(int type) => GameType = type;

        // ============================================================================================
        // §3 篝火/答题/龙魂/菜肴(40255 经验推送/40256 火苗信息/40257 采集火苗奖励/40258 阶段推送/
        // 40259 答题/40260 龙魂信息/40262 战斗结果/40264 购买菜肴/40265 菜肴状态/40266 排名奖励/
        // 40267 经验加成)
        // ============================================================================================

        /// <summary>40255 经验/贡献推送(Type=1,服务端 handle 子句遮蔽导致 Type!=1 分支永不可达——r22 侦察
        /// 已证实,不影响本端接收实现,见 GuildActivityController 类注释)。</summary>
        public int LastExpPushType { get; private set; }
        public long LastExpPushValue { get; private set; }
        public void ApplyExpPush(int type, long exp) { LastExpPushType = type; LastExpPushValue = exp; }

        public sealed class FireInfo { public long Wave; public long NextTime; }
        public FireInfo Fire { get; private set; }
        public void SetFire(FireInfo f) => Fire = f;

        /// <summary>ObjectList 标准条目(Type:8,TypeId:32,Num:**32**,对标 pt:write_object_list)——
        /// 40257/40262/40266 共用,勿与 §1 的 GbossRewardEntry(Num:16 独例)混淆。</summary>
        public sealed class ObjectReward
        {
            public int Type;
            public long TypeId;
            public long Num;
        }

        /// <summary>40257 采集火苗奖励(纯被动推送,c2s"点击火苗"已死——见 GuildActivityController 类注释,
        /// 本端只接收不发送)。</summary>
        public readonly List<ObjectReward> LastFireReward = new List<ObjectReward>();
        public void SetFireReward(List<ObjectReward> list)
        {
            LastFireReward.Clear();
            if (list != null) LastFireReward.AddRange(list);
        }

        /// <summary>40258 阶段推送(老端 on40258 取出即弃,纯占位;本端如实落地供尾包消费,比老端完整无害)。</summary>
        public int LastStage { get; private set; }
        public int LastStageTime { get; private set; }
        public void ApplyStagePush(int stage, int time) { LastStage = stage; LastStageTime = time; }

        public int QuestStatus { get; private set; }
        public void SetQuestStatus(int status) => QuestStatus = status;

        /// <summary>40260 龙魂信息(个人购买 40261 无独立回执,成功走公会广播 40260、失败走 40200——见
        /// GuildActivityController.RequestBuyDragonSpirit 注释)。</summary>
        public long DragonSpirit { get; private set; }
        public bool HasDragonInfo { get; private set; }
        public void SetDragonSpirit(long v) { DragonSpirit = v; HasDragonInfo = true; }

        public sealed class ResultInfo { public int Status; public readonly List<ObjectReward> RewardList = new List<ObjectReward>(); }
        public ResultInfo LastResult { get; private set; }
        public void SetResult(ResultInfo r) => LastResult = r;

        public sealed class FoodEntry { public int Type; public int Status; }
        public readonly List<FoodEntry> FoodList = new List<FoodEntry>();
        public bool HasFoodList { get; private set; }
        public void SetFoodList(List<FoodEntry> list)
        {
            FoodList.Clear();
            if (list != null) FoodList.AddRange(list);
            HasFoodList = true;
        }

        /// <summary>40266 答题积分排名奖励(纯 S-only 推送,无 c2s 对应号——pt_402.erl 无 read(40266) 子句)。</summary>
        public int LastRankRewardRank { get; private set; }
        public readonly List<ObjectReward> LastRankReward = new List<ObjectReward>();
        public void SetRankReward(int rank, List<ObjectReward> list)
        {
            LastRankRewardRank = rank;
            LastRankReward.Clear();
            if (list != null) LastRankReward.AddRange(list);
        }

        public long ExpBuffRatio { get; private set; }
        public bool HasExpBuffRatio { get; private set; }
        public void SetExpBuffRatio(long ratio) { ExpBuffRatio = ratio; HasExpBuffRatio = true; }

        // ============================================================================================
        // §4 族错误出口(40200)
        // ============================================================================================

        public int LastErrorCode { get; private set; }
        public void SetLastError(int code) => LastErrorCode = code;

        // ============================================================================================

        public void Clear()
        {
            Boss = null; HasBoss = false; LastGbossMatAdd = 0;
            LastBossResult = null;
            Act = null; HasAct = false;
            Rank = null; HasRank = false;
            Quest = null;
            MyRank = 0; MyPoint = 0; HasMyRank = false;
            MiniGameFinished = false; HasMiniGameStatus = false;
            GameType = 0;
            LastExpPushType = 0; LastExpPushValue = 0;
            Fire = null;
            LastFireReward.Clear();
            LastStage = 0; LastStageTime = 0;
            QuestStatus = 0;
            DragonSpirit = 0; HasDragonInfo = false;
            LastResult = null;
            FoodList.Clear(); HasFoodList = false;
            LastRankRewardRank = 0; LastRankReward.Clear();
            ExpBuffRatio = 0; HasExpBuffRatio = false;
            LastErrorCode = 0;
        }
    }
}
