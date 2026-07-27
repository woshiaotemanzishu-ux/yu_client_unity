using System.Collections.Generic;

namespace Shenxiao.Module.Core.FirstBlood
{
    /// <summary>
    /// 首杀/首通(FirstBlood)数据(自动循环 轮18 便宜活批 PK3 实做)。pt_188.erl 全 8 号(18800-18807)
    /// 落地容器。**type 收口分发**:96=Boss 首杀(UI 归本模块)/97=副本首通(UI 归 DungeonPartner)/
    /// 105=神符本首通(UI 归 DungeonRune)——本 Model 按 type 分桶存列表/红点,消费方各自按需读取对应桶,
    /// 不在此处再拆分模块。UI 未接,本轮为纯数据层落地。
    /// </summary>
    public sealed class FirstBloodModel
    {
        public static readonly FirstBloodModel Instance = new FirstBloodModel();
        private FirstBloodModel() { }

        // ---- pt_188.erl type 收口(全仓唯一定义,消费方按需引用) ----
        public const int TYPE_BOSS = 96;
        public const int TYPE_DUNGEON = 97;
        public const int TYPE_RUNE = 105;

        /// <summary>18801/18804 共用 DressList 内层嵌套项(pt_188.erl u16×{dress_type:8,dress_id:32})。</summary>
        public struct DressEntry
        {
            public int DressType;
            public int DressId;
        }

        /// <summary>object_list 三元组(pt.erl write_object_list 通用形态,u16 计数 + {Type:8,GoodsId:32,Num:32});
        /// 18802/18807 RewardList 复用此形。</summary>
        public struct RewardObj
        {
            public int Type;
            public int GoodsId;
            public int Num;
        }

        /// <summary>18801 单条(pt_188.erl item_to_bin_0,11 字段:ShowFirstBlood,BossId,FirstBloodRoleId,
        /// RoleName,RoleLv,RoleSex,RoleCarrer,Picture,PictureVer,DressList(二层嵌套),RewardState)。</summary>
        public sealed class ListEntry
        {
            public int ShowFirstBlood;
            public int BossId;
            public long FirstBloodRoleId;
            public string RoleName;
            public int RoleLv;
            public int RoleSex;
            public int RoleCarrer;
            public string Picture;
            public int PictureVer;
            public readonly List<DressEntry> DressList = new List<DressEntry>();
            public int RewardState;
        }

        /// <summary>18804 PassRoleList 单条(pt_188.erl item_to_bin_2,10 字段:RoleId,RoleName,Rank,RoleLv,
        /// RoleSex,RoleCarrer,Picture,PictureVer,DressList(二层嵌套),Time:64)。</summary>
        public sealed class PassRoleEntry
        {
            public long RoleId;
            public string RoleName;
            public int Rank;
            public int RoleLv;
            public int RoleSex;
            public int RoleCarrer;
            public string Picture;
            public int PictureVer;
            public readonly List<DressEntry> DressList = new List<DressEntry>();
            public long Time;
        }

        /// <summary>18805 红点单条(DunId:32, ShowPoint:8)。</summary>
        public struct RedPointEntry
        {
            public int DunId;
            public int ShowPoint;
        }

        /// <summary>18804 神符本(type=105)领奖结果整包(RewardState + PassRoleList)。</summary>
        public sealed class RuneClaimState
        {
            public int RewardState;
            public readonly List<PassRoleEntry> PassRoleList = new List<PassRoleEntry>();
        }

        // ---- 分桶容器(key=type:96/97/105,老端"type 收口分发"镜像) ----
        public readonly Dictionary<int, List<ListEntry>> ListByType = new Dictionary<int, List<ListEntry>>();
        public readonly Dictionary<int, int> ListSubtypeByType = new Dictionary<int, int>();
        public readonly Dictionary<int, List<RedPointEntry>> RedPointByType = new Dictionary<int, List<RedPointEntry>>();

        /// <summary>18806 详情查询结果:key=BossId → SharedStatus。</summary>
        public readonly Dictionary<int, int> SharedStatusByBossId = new Dictionary<int, int>();

        /// <summary>18804 神符本领奖结果:key=DunId。m8存档:老端落桶键是 type@subtype@dun_id 复合键
        /// (FirstBloodModel.ts:131-135 SetRuneFirstData),本端简化为仅 DunId——TYPE_RUNE(105)目前只有
        /// subtype=1 一路数据,DunId 已足够唯一,与老端复合键等价;若未来 105 出现其他 subtype 需要重新评估。</summary>
        public readonly Dictionary<int, RuneClaimState> RuneClaimByDunId = new Dictionary<int, RuneClaimState>();

        // 18803 提醒推送(瞬时,只留最近一条;UI 未接,数据层留档)
        public int NoticeType;
        public int NoticeSubtype;
        public string NoticeRoleName;
        public string NoticeBossName;

        // 18802 个人领奖结果(瞬时,只留最近一条)
        public int LastClaimType;
        public int LastClaimSubtype;
        public int LastClaimCode;
        public int LastClaimBossId;
        public readonly List<RewardObj> LastClaimRewardList = new List<RewardObj>();

        // 18807 全服归属奖结果(瞬时,只留最近一条;与 18802 分开存以免混淆两种奖励语义)
        public int LastGuildClaimType;
        public int LastGuildClaimSubtype;
        public int LastGuildClaimCode;
        public int LastGuildClaimBossId;
        public readonly List<RewardObj> LastGuildClaimRewardList = new List<RewardObj>();

        // 18800 纯推送错误码(瞬时)
        public int LastErrorCode;

        /// <summary>
        /// 对标老端 GetRed(1)：Boss 首杀奖励或全服归属奖励任一未领即显示通知。
        /// 请求开放门槛属于 Controller；通知区只消费已下发的真实状态，不再重复写等级条件。
        /// </summary>
        public bool HasMainNotification()
        {
            if (ListByType.TryGetValue(TYPE_BOSS, out List<ListEntry> entries)
                && ListSubtypeByType.TryGetValue(TYPE_BOSS, out int subtype)
                && subtype == 1)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].RewardState == 0) return true;
                }
            }

            foreach (int sharedStatus in SharedStatusByBossId.Values)
            {
                if (sharedStatus == 0) return true;
            }
            return false;
        }

        /// <summary>断线/登出清空(ControllerHub.DisposeAll 联动)。</summary>
        public void Reset()
        {
            ListByType.Clear();
            ListSubtypeByType.Clear();
            RedPointByType.Clear();
            SharedStatusByBossId.Clear();
            RuneClaimByDunId.Clear();

            NoticeType = 0;
            NoticeSubtype = 0;
            NoticeRoleName = null;
            NoticeBossName = null;

            LastClaimType = 0;
            LastClaimSubtype = 0;
            LastClaimCode = 0;
            LastClaimBossId = 0;
            LastClaimRewardList.Clear();

            LastGuildClaimType = 0;
            LastGuildClaimSubtype = 0;
            LastGuildClaimCode = 0;
            LastGuildClaimBossId = 0;
            LastGuildClaimRewardList.Clear();

            LastErrorCode = 0;
        }
    }
}
