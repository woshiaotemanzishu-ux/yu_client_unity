using System.Collections.Generic;

namespace Shenxiao.Module.Core.Banquet
{
    /// <summary>
    /// 婚宴(婚礼)数据(对标老客户端 BanquetModel)。图标状态(17249/17256)+ 轮24 PB 扩数据层
    /// (17250-17298 间 22 个接收活号；17273 空行为不移植)。裁决4(BanquetModel 自持状态不跨 Model 读写):老端 BanquetModel/
    /// MarriageModel 共享服务端同一份 #marriage_status{}(now_wedding_state/lover_role_id/wedding_pid),
    /// 但 Unity 侧 BanquetModel/MarriageModel 是两个独立单例——本类 NowWeddingState 等字段只由本模块
    /// 17249/17250 自己的协议回包收,不读 MarriageModel 的对应字段,也不写回去(尾包若要联动需显式桥接,
    /// 现状留 open_question,同 r24_banquet.md 侦察稿记录的偏差)。
    /// </summary>
    public sealed class BanquetModel
    {
        public static readonly BanquetModel Instance = new BanquetModel();
        private BanquetModel() { }

        /// <summary>婚礼图标(对标老端 addIcon("172@1"))。婚礼活动开启时显示,配置 open_lv=130、controll_by_own_fun=true。</summary>
        public const string ICON_TYPE_WEDDING = "172@1";

        /// <summary>宾客管理图标(对标老端 addIcon("172@2"))。now_wedding_state==2 时显示,配置 open_lv=0、controll_by_own_fun=true。</summary>
        public const string ICON_TYPE_GUEST = "172@2";

        // 17249 婚礼状态(对标老端 On17249 / banquetModel.banqState)
        public int BanqState;       // 上一次 now_wedding_state,供 172@2 的 tri-state 增删判定(2→0 才删)
        public int NowWeddingState; // 当前 now_wedding_state:0 无婚宴 / 2 婚宴报名开启(其余状态图标不动)
        public int BeginTime;       // 婚宴开始时间戳(老端据此算图标倒计时文本,本期图标只做显隐,读存不用于判定)

        // 17256 婚礼召集(对标老端 SetBanquetCall / weddingInfo)
        public bool WeddingActive;  // type==1 且 wedding_list 非空 → 有婚礼进行/待开始

        /// <summary>172@2 宾客管理入口开启(对标老端 On17249:now_wedding_state==2)。</summary>
        public bool GetGuestIconOpen()
        {
            return NowWeddingState == 2;
        }

        /// <summary>172@1 婚礼入口开启(对标老端 SetBanquetCall:type==1 且 wedding_list 非空)。</summary>
        public bool GetWeddingIconOpen()
        {
            return WeddingActive;
        }

        // =====================================================================================
        // 轮24 PB 数据层扩展(17250-17298)
        // =====================================================================================

        // ---- 17250 预约/报名视图数据(对标老端 ApplyViewData/canApply) ----

        public sealed class WeddingTimesEntry { public int WeddingType; public int UseTimes; public int MaxTimes; public bool OrderToday; }
        public sealed class DayOrderEntry { public long RoleIdM; public long RoleIdW; public int WeddingType; public bool IfOwn; }
        public sealed class TimeSlotEntry { public int TimeId; public readonly List<DayOrderEntry> OrderList = new List<DayOrderEntry>(); }
        public sealed class DayEntry { public long OrderUnixDate; public readonly List<TimeSlotEntry> TimeList = new List<TimeSlotEntry>(); }

        public sealed class ApplyViewInfo
        {
            public int NowWeddingState;
            public readonly List<WeddingTimesEntry> MyWeddingTimes = new List<WeddingTimesEntry>();
            public readonly List<DayEntry> DayList = new List<DayEntry>();
        }

        public ApplyViewInfo ApplyView;
        /// <summary>对标老端 banquetModel.canApply:now_wedding_state==2 时恒 false;否则任一 wedding_type
        /// use_times&lt;max_times 且(非3型 或 3型未 order_today)即 true(见 SetApplyViewData)。</summary>
        public bool CanApply;

        public void SetApplyViewData(ApplyViewInfo info)
        {
            ApplyView = info;
            CanApply = false;
            if (info == null || info.NowWeddingState == 2) return;
            foreach (WeddingTimesEntry vo in info.MyWeddingTimes)
            {
                if (vo.UseTimes < vo.MaxTimes && (vo.WeddingType != 3 || (vo.WeddingType == 3 && !vo.OrderToday)))
                {
                    CanApply = true;
                }
            }
        }

        // ---- 17252 邀请视图数据 + 顶层共享桶(GuestList/AskData,与 17260 共写,对标老端裁决见类注释) ----

        /// <summary>宾客条目(17252 GuestList / 17260 type==2 InfoList 共用形状:RoleId,AnswerType,Name)。</summary>
        public sealed class GuestEntry { public long RoleId; public int AnswerType; public string Name = ""; }

        /// <summary>索要条目。17252 ask_invite_list 只有 RoleId/Name(AnswerType 恒 -1=未知);
        /// 17260 type==1 InfoList 三字段齐全(RoleId/AnswerType/Name)——两个来源形状不同,统一成同一类型,
        /// AnswerType=-1 标记"该条来自 17252,答复状态未知"。</summary>
        public sealed class AskEntry { public long RoleId; public string Name = ""; public int AnswerType = -1; }

        public sealed class InviteViewInfo
        {
            public long MyRoleId; public string MyName = ""; public string MyPicture = ""; public long MyPictureVer;
            public long LoverRoleId; public string LoverName = ""; public string LoverPicture = ""; public long LoverPictureVer;
            public int WeddingType; public long WeddingTime; public bool IfOrderAgain; public int LessInviteNum; public int GuestNum;
            public readonly List<GuestEntry> GuestList = new List<GuestEntry>();
            public readonly List<AskEntry> AskInviteList = new List<AskEntry>();
        }

        public InviteViewInfo InviteView;

        /// <summary>顶层宾客桶(对标老端 banquetModel.guestList,17252/17260 type==2 共写同一个字段)。</summary>
        public readonly List<GuestEntry> GuestList = new List<GuestEntry>();
        /// <summary>顶层索要桶(对标老端 banquetModel.AskData,17252(经 SetAskData)/17260 type==1 共写)。
        /// null 与"空列表"语义不同(对标老端 `!banquetModel.AskData` 判定"从未收到过"),用 <see cref="HasAskData"/> 区分。</summary>
        public List<AskEntry> AskData;
        public bool HasAskData => AskData != null;
        /// <summary>对标老端 banquetModel.newApply(172@2 红点"是否为新申请"标记,仅 17260 type==1 分支更新)。</summary>
        public bool NewApply;
        /// <summary>对标老端 banquetModel.lessInviteNum,**仅 17260 写**(17252 虽也有 LessInviteNum 字段,但
        /// 老端只塞进 InviteView,不写顶层这个字段——本端镜像该不对称)。</summary>
        public int LessInviteNum;

        public void SetInviteViewData(InviteViewInfo info)
        {
            InviteView = info;
            if (info == null) return;
            GuestList.Clear();
            GuestList.AddRange(info.GuestList);
            SetAskData(info.AskInviteList);
        }

        /// <summary>对标老端 SetAskData(简单计数判定,17252 专用路径;17260 type==1 走更精细的
        /// "比上次更多才算新"判定,见 BanquetController.On17260)。</summary>
        public void SetAskData(List<AskEntry> info)
        {
            AskData = info ?? new List<AskEntry>();
            NewApply = AskData.Count > 0;
        }

        // ---- 17262 婚礼动画场景信息 ----

        public sealed class ScenePersonEntry { public long RoleId; public Shenxiao.Common.Proto.FigureProto Figure; }
        public sealed class GuestPositionEntry { public int PosId; public long GuestRoleId; public bool IfEnter; }

        public sealed class WeddingRoleListInfo
        {
            public readonly List<ScenePersonEntry> ManList = new List<ScenePersonEntry>();
            public readonly List<ScenePersonEntry> WomanList = new List<ScenePersonEntry>();
            public readonly List<GuestPositionEntry> GuestPositionList = new List<GuestPositionEntry>();
        }

        public WeddingRoleListInfo WeddingRoleList;

        // ---- 17265 婚礼信息(对标老端 BanquetData) ----

        public sealed class WeddingSceneInfo
        {
            public int StageId; public long StageEndTime; public long Aura;
            public long LessNormalCandies; public long LessSpecialCandies; public int GuestsNum;
        }

        public WeddingSceneInfo BanquetData;

        // ---- 17272 婚礼道具使用信息(对标老端 GoodsInfo/list_table_num) ----

        public sealed class GoodsInfoData
        {
            public bool IfMaster; public int FreeCandies; public int FreeFires;
            public readonly List<long> CollectTableList = new List<long>();
        }

        public GoodsInfoData GoodsInfo;
        /// <summary>已采集餐桌 id 集合(对标老端 list_table_num,"monster_instance_id"→已采集)。</summary>
        public readonly Dictionary<long, bool> ListTableNum = new Dictionary<long, bool>();

        public void ApplyGoodsInfo(GoodsInfoData info)
        {
            GoodsInfo = info;
            if (info == null || BanquetData == null || BanquetData.StageId != 3) return;
            foreach (long tableId in info.CollectTableList) ListTableNum[tableId] = true;
        }

        // ---- 17275/17277/17278/17279 杂项推送 ----

        /// <summary>对标 pt:write_object_list 单条(Type:8,TypeId:32,Num:32),Banquet 自持一份不跨 Model 引用
        /// (裁决4 同一原则:类型定义也不共享 Marriage 的 RewardEntry)。</summary>
        public sealed class RewardEntry { public int Type; public long TypeId; public long Num; }

        public long AllExp;            // 17275
        public long AuraValue;         // 17277(Type==1 时落地)
        public long LastAuraNum;       // 17278 AuraNum
        public readonly List<RewardEntry> LastAuraReward = new List<RewardEntry>();   // 17278 Reward
        public int LastTableRewardType; // 17279 Type
        public readonly List<RewardEntry> LastTableReward = new List<RewardEntry>();  // 17279 Reward

        public void Reset()
        {
            BanqState = 0;
            NowWeddingState = 0;
            BeginTime = 0;
            WeddingActive = false;

            ApplyView = null;
            CanApply = false;
            InviteView = null;
            GuestList.Clear();
            AskData = null;
            NewApply = false;
            LessInviteNum = 0;
            WeddingRoleList = null;
            BanquetData = null;
            GoodsInfo = null;
            ListTableNum.Clear();
            AllExp = 0;
            AuraValue = 0;
            LastAuraNum = 0;
            LastAuraReward.Clear();
            LastTableRewardType = 0;
            LastTableReward.Clear();
        }
    }
}
