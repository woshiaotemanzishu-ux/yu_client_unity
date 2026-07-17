using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// P4 节日族(自动循环 轮17,spec §5,20号):
    ///   摇钱树 MONEYTREE(50)/MOUNT_TURNTABLE(54)/MONEYTREE_SHOP(89): 33190/33191/33192/33168/33231
    ///   FTVACTIVENESS(56): 33193/33194/33195/33196(recv-only广播)
    ///   SAIBOTREASURE(58,赛博夺宝): 33165/33166/33167(本包嵌套最深:StageS→GradeState 三层)
    ///   绑钻转盘 TURNTABLE(28): 33130/33131/33132
    ///   RED_PACKET_RAIN(82,红包雨): 33155(无 BaseType)/33157(无 BaseType)/33158(recv-only 3字段)
    ///   HOLYCALL(67,神圣召唤): 33221(四嵌套+RareDrawTimes 尾字段)/33222
    ///
    /// wire 全部逐字段回 pt_331.erl/pt_332.erl 原文核对(write 子句 + item_to_bin_N + 对应 pp_custom_act(_list).erl
    /// handle 子句的 C2S read 参数),不采信 r17_server 侦察表的简写。本轮核出的与侦察表不同之处(供三镜头验收复核):
    ///  - **33191 C2S 的 Times 是 8 位**(pt_331.erl:315 `Tiems:8`,变量名原文笔误但类型不误),不是侦察表写的 16 位;
    ///    S2C write **ErrorCode 领先**(pt_331.erl:1961 `write(33191,[ErrorCode,...])`),侦察表"首字段Code32=N"有误。
    ///  - 33190 的 CumulateReward[] 结构是 {GradeId,Times,Reward,Status}(item_to_bin_65),与 ShowList[] 的
    ///    {GradeId,IsRare,Reward}(item_to_bin_64)不同,侦察表"(同结构)"是误记;33190 S2C 本身**也带 ErrorCode**
    ///    (第3字段,pt_331.erl:1916-1959),按错误码结果分支解析,不是纯推送。
    ///  - 33165(赛博夺宝界面)Pool[] 元素(item_to_bin_43)字段序 **Reward=Obj[] 在最前**,GradeId/IsRare/Sort/State
    ///    在后;StageS[] 是三层嵌套 StageS→GradeState→{GradeReward,BuyReward} 两个奖励数组。
    ///  - 33167 RewardList[] 元素(item_to_bin_46)比侦察表多一个尾字段 Sort:8。
    ///  - 33130(绑钻转盘界面)NTimesList[]/RewardList[] 是**两个平级数组**(pt_331.erl:850-886 Data 二进制逐字段核实),
    ///    不是侦察表箭头写法暗示的嵌套;33130/33131/33132 三号均**无 ErrorCode 字段**,纯数据推送。
    ///  - 33132(转盘记录)RoleId 是 **32 位**(item_to_bin_28),非本框架内常见的 64 位 RoleId。
    ///  - 33155/33157 的 C2S read 均**只有 SubType,无 BaseType**(pt_331.erl:183-188),对标老端 fmt 表特例
    ///    (Core.cs RequestActDetail 的 ACT_ID_RED_PACKET_RAIN 分支已按此发 "h" 单参,本文件的 RequestRedRain*
    ///    方法保持同样单参签名)。
    ///  - 疑似线上死路径(存档不改实现,**三镜头订正**):pp_custom_act.erl:1290 对 33190 的失败分支
    ///    `lib_server_send:send_to_sid(Sid, pt_331, 33190, [Type, SubType, Code, 0, 0, [], []])` 只给 7 个字段,
    ///    与 write(33190,...) 定义的 9 元组模式不匹配——但**不会 function_clause 崩溃**:pt_331.erl:2161
    ///    `write(_Cmd, _R) -> ?DEBUG(...), {ok, pt:pack(0, <<>>)}` 是兜底子句,任何字段数不匹配的调用都落到
    ///    这里,静默发一个 cmd=0 的空包(不崩)。类似 spec §1 已记录的 33158 lib_red_envelopes_mod.erl:302
    ///    错用字段数 bug,同样落这个兜底、不崩溃。本端不因此改变对 33190 有效包(9字段)的解析结构。
    ///
    /// handler 规矩(spec §8):先落 Model 后 Emit;ErrorCode 存在的号——失败 ShowError 显码降级+不覆盖 Model,
    /// 成功落 Model,统一 Emit EVT_CUSTOMACT_RESULT(baseType,subType,code);无 ErrorCode 的纯推送号——总是落地,
    /// Emit EVT_CUSTOMACT_DETAIL_UPDATE(baseType,subType)。GlobalEvent.cs 是 P1 独占的共享文件,P4 不新增事件,
    /// 一律复用 P1 已定义的通用事件(仅红包雨新波次推送 33158 按 spec 明确指示复用 EVT_CUSTOMACT_REDPACKET_WAVE)。
    /// recv-only 号(33196/33158)只注册防御 recv,不写发送方法。
    /// </summary>
    public sealed partial class CustomActivityController
    {
        /// <summary>P1 预建空壳,由主文件 Register() 调用。</summary>
        private void RegisterFestival()
        {
            RegisterProtocal(Proto.CUSTOM_ACT_MONEYTREE_PANEL, On33190);
            RegisterProtocal(Proto.CUSTOM_ACT_MONEYTREE_DRAW, On33191);
            RegisterProtocal(Proto.CUSTOM_ACT_MONEYTREE_CUMULATE, On33192);
            RegisterProtocal(Proto.CUSTOM_ACT_MONEYTREE_SHOP, On33168);
            RegisterProtocal(Proto.CUSTOM_ACT_MONEYTREE_CURRENCY, On33231);

            RegisterProtocal(Proto.CUSTOM_ACT_FTVACTIVE_PANEL, On33193);
            RegisterProtocal(Proto.CUSTOM_ACT_FTVACTIVE_SUBMIT, On33194);
            RegisterProtocal(Proto.CUSTOM_ACT_FTVACTIVE_SERVER_CLAIM, On33195);
            RegisterProtocal(Proto.CUSTOM_ACT_FTVACTIVE_TRIGGER_PUSH, On33196); // recv-only

            RegisterProtocal(Proto.CUSTOM_ACT_SAIBO_PANEL, On33165);
            RegisterProtocal(Proto.CUSTOM_ACT_SAIBO_STAGE, On33166);
            RegisterProtocal(Proto.CUSTOM_ACT_SAIBO_DRAW, On33167);

            RegisterProtocal(Proto.CUSTOM_ACT_BINDDIAMOND_PANEL, On33130);
            RegisterProtocal(Proto.CUSTOM_ACT_BINDDIAMOND_DRAW, On33131);
            RegisterProtocal(Proto.CUSTOM_ACT_BINDDIAMOND_RECORD, On33132);

            RegisterProtocal(Proto.CUSTOM_ACT_REDRAIN_PANEL, On33155);
            RegisterProtocal(Proto.CUSTOM_ACT_REDRAIN_GRAB, On33157);
            RegisterProtocal(Proto.CUSTOM_ACT_REDRAIN_WAVE_PUSH, On33158); // recv-only

            RegisterProtocal(Proto.CUSTOM_ACT_HOLYCALL_PANEL, On33221);
            RegisterProtocal(Proto.CUSTOM_ACT_HOLYCALL_RARE_DRAW, On33222);
        }

        // ---------------------------------------------------------------------------------------
        // §1 摇钱树 MONEYTREE(50)/MOUNT_TURNTABLE(54)/MONEYTREE_SHOP(89)
        // ---------------------------------------------------------------------------------------

        /// <summary>33190 摇钱树界面(ErrorCode 是第 3 字段,非开头非纯推送)。**静默阈值镜像**(ts:1546-1550):
        /// 失败时 code==1012 或 code==3310043 不弹错。</summary>
        private void On33190(NetReader r)
        {
            CustomActivityModel.MoneyTreePanelData d = CustomActivityModel.ReadMoneyTreePanel(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetMoneyTreePanel(d);
            else if (d.ErrorCode != 1012 && d.ErrorCode != 3310043) ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33190 摇钱树界面 base={0} sub={1} code={2} showN={3} cumulateN={4} shopN={5}",
                d.BaseType, d.SubType, d.ErrorCode, d.ShowList.Count, d.CumulateReward.Count, d.Shop.Count);
        }

        /// <summary>33191 摇钱树抽奖(服务端同号双子句 HOLY_SUMMON 精确+通用兜底,Unity 单 handler 按字段解析即可;
        /// ErrorCode 领先)。**静默阈值镜像**(ts:1558-1562):失败时 code==1012 不弹错。</summary>
        private void On33191(NetReader r)
        {
            CustomActivityModel.MoneyTreeDrawResult d = CustomActivityModel.ReadMoneyTreeDrawResult(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetMoneyTreeDrawResult(d);
            else if (d.ErrorCode != 1012) ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33191 摇钱树抽奖 base={0} sub={1} code={2} rewardN={3} score={4}",
                d.BaseType, d.SubType, d.ErrorCode, d.RewardList.Count, d.Score);
        }

        /// <summary>33192 摇钱树累计奖励领取(服务端同号双子句同 33191)。**静默阈值镜像**(ts:1569-1573):
        /// 失败时 code==1012 不弹错。</summary>
        private void On33192(NetReader r)
        {
            CustomActivityModel.MoneyTreeCumulateResult d = CustomActivityModel.ReadMoneyTreeCumulateResult(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetMoneyTreeCumulateResult(d);
            else if (d.ErrorCode != 1012) ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33192 摇钱树累计奖励 base={0} sub={1} code={2} cumulateN={3}",
                d.BaseType, d.SubType, d.ErrorCode, d.CumulateReward.Count);
        }

        /// <summary>33168 树商店兑换(ErrorCode 在第 4 位)。**静默阈值镜像**(ts:1580-1584):失败时 code==1012
        /// 不弹错。</summary>
        private void On33168(NetReader r)
        {
            CustomActivityModel.MoneyTreeShopResult d = CustomActivityModel.ReadMoneyTreeShopResult(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetMoneyTreeShopResult(d);
            else if (d.ErrorCode != 1012) ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33168 树商店兑换 base={0} sub={1} grade={2} code={3} rewardN={4} num={5} score={6}",
                d.BaseType, d.SubType, d.GradeId, d.ErrorCode, d.Reward.Count, d.Num, d.Score);
        }

        /// <summary>33231 契约点/货币展示(纯推送,无 ErrorCode)。</summary>
        private void On33231(NetReader r)
        {
            CustomActivityModel.MoneyTreeCurrency d = CustomActivityModel.ReadMoneyTreeCurrency(r);
            CustomActivityModel.Instance.SetMoneyTreeCurrency(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, d.BaseType, d.SubType);
            GameLog.Info("CustomActivity", "33231 摇钱树货币 base={0} sub={1} currency={2}", d.BaseType, d.SubType, d.Currency);
        }

        /// <summary>33190 请求界面(read: BaseType,SubType)。</summary>
        public void RequestMoneyTreePanel(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_MONEYTREE_PANEL, "hh", baseType, subType);

        /// <summary>33191 抽奖(read: BaseType,SubType,Times:8,AutoBuy:8——Times **8位**,pt_331.erl:315)。</summary>
        public void RequestMoneyTreeDraw(int baseType, int subType, int times, int autoBuy) =>
            SendFmt(Proto.CUSTOM_ACT_MONEYTREE_DRAW, "hhcc", baseType, subType, times, autoBuy);

        /// <summary>33192 累计奖励领取(read: BaseType,SubType,GradeId:16)。</summary>
        public void RequestMoneyTreeCumulateClaim(int baseType, int subType, int gradeId) =>
            SendFmt(Proto.CUSTOM_ACT_MONEYTREE_CUMULATE, "hhh", baseType, subType, gradeId);

        /// <summary>33168 树商店兑换(read: BaseType,SubType,GradeId:16)。</summary>
        public void RequestMoneyTreeShopExchange(int baseType, int subType, int gradeId) =>
            SendFmt(Proto.CUSTOM_ACT_MONEYTREE_SHOP, "hhh", baseType, subType, gradeId);

        /// <summary>33231 货币展示(read: BaseType,SubType)。</summary>
        public void RequestMoneyTreeCurrency(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_MONEYTREE_CURRENCY, "hh", baseType, subType);

        // ---------------------------------------------------------------------------------------
        // §2 FTVACTIVENESS(56,节日活跃)
        // ---------------------------------------------------------------------------------------

        /// <summary>33193 节日活跃界面(纯推送,无 ErrorCode)。</summary>
        private void On33193(NetReader r)
        {
            CustomActivityModel.FtvActivePanelData d = CustomActivityModel.ReadFtvActivePanel(r);
            CustomActivityModel.Instance.SetFtvActivePanel(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, d.BaseType, d.SubType);
            GameLog.Info("CustomActivity", "33193 节日活跃界面 base={0} sub={1} personTimes={2} serverTimes={3} rewardN={4}",
                d.BaseType, d.SubType, d.PersonTimes, d.ServerTimes, d.SerRewardList.Count);
        }

        /// <summary>33194 节日活跃提交(ErrorCode 领先)。</summary>
        private void On33194(NetReader r)
        {
            CustomActivityModel.FtvActiveSubmitResult d = CustomActivityModel.ReadFtvActiveSubmitResult(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetFtvActiveSubmitResult(d);
            else ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33194 节日活跃提交 base={0} sub={1} costType={2} code={3} rewardN={4} personTimes={5}",
                d.BaseType, d.SubType, d.CostType, d.ErrorCode, d.RewardList.Count, d.PersonTimes);
        }

        /// <summary>33195 节日活跃领取全服奖励(ErrorCode 领先)。</summary>
        private void On33195(NetReader r)
        {
            CustomActivityModel.FtvActiveServerClaimResult d = CustomActivityModel.ReadFtvActiveServerClaimResult(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetFtvActiveServerClaimResult(d);
            else ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33195 节日活跃全服奖励 base={0} sub={1} grade={2} code={3} rewardN={4}",
                d.BaseType, d.SubType, d.GradeId, d.ErrorCode, d.Reward.Count);
        }

        /// <summary>33196 **recv-only** 触发类型广播(老端 lib_custom_act_liveness_mod.erl:95 推送,无 ErrorCode)。
        /// **追发镜像**(ts:1657-1676,自动循环 轮17三镜头验收补):老端仅当 GetActData(base,sub) 命中(即该
        /// 活动已有 33104 通用详情落地,对应本端 CustomActivityModel.GetDetail)且 is_ask!=0 时才追发 33193
        /// 重拉节日活跃界面(随即 return,不做其它事)。</summary>
        private void On33196(NetReader r)
        {
            CustomActivityModel.FtvActiveTriggerPush d = CustomActivityModel.ReadFtvActiveTriggerPush(r);
            CustomActivityModel.Instance.SetFtvActiveTriggerPush(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, d.BaseType, d.SubType);
            if (d.IsAsk != 0 && CustomActivityModel.Instance.GetDetail(d.BaseType, d.SubType) != null)
            {
                RequestFtvActivePanel(d.BaseType, d.SubType); // 追发 33193,镜像 ts:1672
            }
            GameLog.Info("CustomActivity", "33196 节日活跃触发广播(recv-only) base={0} sub={1} serverTimes={2} isAsk={3} triggerN={4}",
                d.BaseType, d.SubType, d.ServerTimes, d.IsAsk, d.TriggerTypeList.Count);
        }

        /// <summary>33193 请求界面(read: BaseType,SubType)。</summary>
        public void RequestFtvActivePanel(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_FTVACTIVE_PANEL, "hh", baseType, subType);

        /// <summary>33194 提交(read: BaseType,SubType,CostType:8)。</summary>
        public void RequestFtvActiveSubmit(int baseType, int subType, int costType) =>
            SendFmt(Proto.CUSTOM_ACT_FTVACTIVE_SUBMIT, "hhc", baseType, subType, costType);

        /// <summary>33195 领取全服奖励(read: BaseType,SubType,GradeId:16)。</summary>
        public void RequestFtvActiveServerClaim(int baseType, int subType, int gradeId) =>
            SendFmt(Proto.CUSTOM_ACT_FTVACTIVE_SERVER_CLAIM, "hhh", baseType, subType, gradeId);

        // 33196 recv-only,严禁写发送方法。

        // ---------------------------------------------------------------------------------------
        // §3 SAIBOTREASURE(58,赛博夺宝)—— 本包嵌套最深(StageS→GradeState 三层)
        // ---------------------------------------------------------------------------------------

        /// <summary>33165 赛博夺宝界面(纯推送,无 ErrorCode)。</summary>
        private void On33165(NetReader r)
        {
            CustomActivityModel.SaiboPanelData d = CustomActivityModel.ReadSaiboPanel(r);
            CustomActivityModel.Instance.SetSaiboPanel(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, d.BaseType, d.SubType);
            GameLog.Info("CustomActivity", "33165 赛博夺宝界面 base={0} sub={1} wave={2} allTimes={3} today={4} poolN={5} stageN={6}",
                d.BaseType, d.SubType, d.Wave, d.AllTimes, d.TodayDrawtimes, d.Pool.Count, d.StageS.Count);
        }

        /// <summary>33166 赛博夺宝阶段奖励(ErrorCode 开头,末尾还有 Buy 字段)。</summary>
        private void On33166(NetReader r)
        {
            CustomActivityModel.SaiboStageResult d = CustomActivityModel.ReadSaiboStageResult(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetSaiboStageResult(d);
            else ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33166 赛博夺宝阶段奖励 base={0} sub={1} stage={2} gradeStage={3} code={4} rewardN={5} buy={6}",
                d.BaseType, d.SubType, d.Stage, d.GradeStage, d.ErrorCode, d.Reward.Count, d.Buy);
        }

        /// <summary>33167 赛博夺宝抽奖(ErrorCode 开头)。</summary>
        private void On33167(NetReader r)
        {
            CustomActivityModel.SaiboDrawResult d = CustomActivityModel.ReadSaiboDrawResult(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetSaiboDrawResult(d);
            else ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33167 赛博夺宝抽奖 base={0} sub={1} code={2} allTimes={3} today={4} rewardN={5}",
                d.BaseType, d.SubType, d.ErrorCode, d.AllTimes, d.TodayDrawtimes, d.RewardList.Count);
        }

        /// <summary>33165 请求界面(read: BaseType,SubType)。</summary>
        public void RequestSaiboPanel(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_SAIBO_PANEL, "hh", baseType, subType);

        /// <summary>33166 阶段奖励领取(read: BaseType,SubType,Stage:8,GradeStage:8,Buy:8)。</summary>
        public void RequestSaiboStage(int baseType, int subType, int stage, int gradeStage, int buy) =>
            SendFmt(Proto.CUSTOM_ACT_SAIBO_STAGE, "hhccc", baseType, subType, stage, gradeStage, buy);

        /// <summary>33167 抽奖(read: BaseType,SubType,Times:8,AutoBuy:8)。</summary>
        public void RequestSaiboDraw(int baseType, int subType, int times, int autoBuy) =>
            SendFmt(Proto.CUSTOM_ACT_SAIBO_DRAW, "hhcc", baseType, subType, times, autoBuy);

        // ---------------------------------------------------------------------------------------
        // §4 绑钻转盘 TURNTABLE(28)—— 33130/33131/33132 三号均无 ErrorCode,纯数据推送
        // ---------------------------------------------------------------------------------------

        /// <summary>33130 绑钻转盘界面(NTimesList[]+RewardList[] 两个平级数组,无 ErrorCode)。</summary>
        private void On33130(NetReader r)
        {
            CustomActivityModel.BindDiamondPanelData d = CustomActivityModel.ReadBindDiamondPanel(r);
            CustomActivityModel.Instance.SetBindDiamondPanel(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, d.BaseType, d.SubType);
            GameLog.Info("CustomActivity", "33130 绑钻转盘界面 base={0} sub={1} ticket={2} nTimesN={3} rewardN={4}",
                d.BaseType, d.SubType, d.TicketNum, d.NTimesList.Count, d.RewardList.Count);
        }

        /// <summary>33131 绑钻转盘抽奖结果(无 ErrorCode,C2S read 只有 BaseType,SubType 两个字段——服务端自行判定
        /// 花费与结果,不接受客户端指定次数)。</summary>
        private void On33131(NetReader r)
        {
            CustomActivityModel.BindDiamondDrawResult d = CustomActivityModel.ReadBindDiamondDrawResult(r);
            CustomActivityModel.Instance.SetBindDiamondDrawResult(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, d.BaseType, d.SubType);
            GameLog.Info("CustomActivity", "33131 绑钻转盘抽奖 base={0} sub={1} goods={2}x{3} nTimes={4} ticket={5} totalLeft={6}",
                d.BaseType, d.SubType, d.GoodsId, d.GoodsNum, d.NTimes, d.TicketNum, d.TotalLeftTickets);
        }

        /// <summary>33132 绑钻转盘记录(无 ErrorCode;List[] 元素 RoleId 是 **32 位**,item_to_bin_28)。</summary>
        private void On33132(NetReader r)
        {
            CustomActivityModel.BindDiamondRecordData d = CustomActivityModel.ReadBindDiamondRecord(r);
            CustomActivityModel.Instance.SetBindDiamondRecord(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, d.BaseType, d.SubType);
            GameLog.Info("CustomActivity", "33132 绑钻转盘记录 base={0} sub={1} listN={2}", d.BaseType, d.SubType, d.List.Count);
        }

        /// <summary>33130 请求界面(read: BaseType,SubType)。</summary>
        public void RequestBindDiamondPanel(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_BINDDIAMOND_PANEL, "hh", baseType, subType);

        /// <summary>33131 抽奖(read: BaseType,SubType,无次数/自动购买参数)。</summary>
        public void RequestBindDiamondDraw(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_BINDDIAMOND_DRAW, "hh", baseType, subType);

        /// <summary>33132 请求记录(read: BaseType,SubType)。</summary>
        public void RequestBindDiamondRecord(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_BINDDIAMOND_RECORD, "hh", baseType, subType);

        // ---------------------------------------------------------------------------------------
        // §5 RED_PACKET_RAIN(82,红包雨)—— 33155/33157 的 C2S/S2C 均无 BaseType,只有 SubType
        // ---------------------------------------------------------------------------------------

        /// <summary>33155 红包雨界面(WaveReceive 嵌套,无 BaseType 无 ErrorCode)。</summary>
        private void On33155(NetReader r)
        {
            CustomActivityModel.RedRainPanelData d = CustomActivityModel.ReadRedRainPanel(r);
            CustomActivityModel.Instance.SetRedRainPanel(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, ACT_ID_RED_PACKET_RAIN, d.SubType);
            GameLog.Info("CustomActivity", "33155 红包雨界面 sub={0} actValue={1} wave={2} waveReceiveN={3}",
                d.SubType, d.ActValue, d.Wave, d.WaveReceive.Count);
        }

        /// <summary>33157 抢红包(Errcode 领先,无 BaseType)。**回写面板镜像**(ts:2218-2239,仿同文件
        /// 33129/33133 回写先例):成功后若已缓存 33155 面板(RedRainPanelData),回写 Wave,并把 WaveReceive
        /// 里 Wave2==本次 Wave 的那条 IsReceive 置 1(老端 `if(!adata) return`——面板未缓存时不回写,不落
        /// null 引用)。**核对偏差存档**:老端该处还有 `adata.start_time = scmd.start_time`,但服务端
        /// pt_331.erl write(33157):1282-1296 真实只有 Errcode/SubType/Wave/Rewards 四字段、无 start_time——
        /// 老端这一行实际把 undefined 写进 adata.start_time(JS 弱类型静默接受),是老端自身的无效赋值,
        /// 本端没有等价可镜像的字段来源,不引入这个赋值(保留面板原 StartTime 不动)。</summary>
        private void On33157(NetReader r)
        {
            CustomActivityModel.RedRainGrabResult d = CustomActivityModel.ReadRedRainGrabResult(r);
            if (d.Errcode == 1)
            {
                CustomActivityModel.Instance.SetRedRainGrabResult(d);
                CustomActivityModel.RedRainPanelData panel = CustomActivityModel.Instance.GetRedRainPanel(d.SubType);
                if (panel != null)
                {
                    panel.Wave = d.Wave;
                    for (int i = 0; i < panel.WaveReceive.Count; i++)
                    {
                        if (panel.WaveReceive[i].Wave2 == d.Wave) { panel.WaveReceive[i].IsReceive = 1; break; }
                    }
                }
            }
            else ShowError(d.Errcode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, ACT_ID_RED_PACKET_RAIN, d.SubType, d.Errcode);
            GameLog.Info("CustomActivity", "33157 抢红包 sub={0} code={1} wave={2} rewardN={3}", d.SubType, d.Errcode, d.Wave, d.Rewards.Count);
        }

        /// <summary>33158 **recv-only** 新波次开始推送(SubType,Wave,StartTime 3字段;服务端
        /// lib_red_envelopes_mod.erl:302 存在错用16字段调用本号的线上 bug[应为33902],与本号定义结构无关,本端
        /// 只按这里的权威 3 字段解析)。spec 明确指示复用 EVT_CUSTOMACT_REDPACKET_WAVE(P1 已建,不新增事件)。
        /// **追发镜像**(ts:2246-2248,自动循环 轮17三镜头验收补):wave==1 时追发 33155 重拉红包雨界面。</summary>
        private void On33158(NetReader r)
        {
            CustomActivityModel.RedRainWavePush d = CustomActivityModel.ReadRedRainWavePush(r);
            CustomActivityModel.Instance.SetRedRainWavePush(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_REDPACKET_WAVE, d.SubType, d.Wave, d.StartTime);
            if (d.Wave == 1)
            {
                RequestRedRainPanel(d.SubType); // 追发 33155,镜像 ts:2247
            }
            GameLog.Info("CustomActivity", "33158 红包雨新波次推送(recv-only) sub={0} wave={1} startTime={2}", d.SubType, d.Wave, d.StartTime);
        }

        /// <summary>33155 请求界面(read: **仅 SubType**,无 BaseType;对标老端 fmt 表特例,Core.cs
        /// RequestActDetail 的 ACT_ID_RED_PACKET_RAIN 分支已发送同样的单参包,本方法供 UI 直接调用)。</summary>
        public void RequestRedRainPanel(int subType) => SendFmt(Proto.CUSTOM_ACT_REDRAIN_PANEL, "h", subType);

        /// <summary>33157 抢红包(read: **仅 SubType**,无 BaseType/Wave)。</summary>
        public void RequestRedRainGrab(int subType) => SendFmt(Proto.CUSTOM_ACT_REDRAIN_GRAB, "h", subType);

        // 33158 recv-only,严禁写发送方法。

        // ---------------------------------------------------------------------------------------
        // §6 HOLYCALL(67,神圣召唤)
        // ---------------------------------------------------------------------------------------

        /// <summary>33221 神圣召唤信息(四嵌套:ShowList/CumulateReward/RarePool + 尾字段 RareDrawTimes;
        /// ErrorCode 是第 3 字段)。**静默阈值镜像**(ts:2123-2127):失败时 code==1012 或 code==3310043
        /// 不弹错。</summary>
        private void On33221(NetReader r)
        {
            CustomActivityModel.HolyCallPanelData d = CustomActivityModel.ReadHolyCallPanel(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetHolyCallPanel(d);
            else if (d.ErrorCode != 1012 && d.ErrorCode != 3310043) ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33221 神圣召唤信息 base={0} sub={1} code={2} showN={3} cumulateN={4} rareN={5} rareDrawTimes={6}",
                d.BaseType, d.SubType, d.ErrorCode, d.ShowList.Count, d.CumulateReward.Count, d.RarePool.Count, d.RareDrawTimes);
        }

        /// <summary>33222 神圣召唤稀有抽(ErrorCode 领先)。**静默阈值镜像**(ts:2135-2139):失败时 code==1012
        /// 不弹错。</summary>
        private void On33222(NetReader r)
        {
            CustomActivityModel.HolyCallRareDrawResult d = CustomActivityModel.ReadHolyCallRareDrawResult(r);
            if (d.ErrorCode == 1) CustomActivityModel.Instance.SetHolyCallRareDrawResult(d);
            else if (d.ErrorCode != 1012) ShowError(d.ErrorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, d.BaseType, d.SubType, d.ErrorCode);
            GameLog.Info("CustomActivity", "33222 神圣召唤稀有抽 base={0} sub={1} code={2} rareDrawTimes={3} rewardN={4}",
                d.BaseType, d.SubType, d.ErrorCode, d.RareDrawTimes, d.RewardList.Count);
        }

        /// <summary>33221 请求信息(read: BaseType,SubType)。</summary>
        public void RequestHolyCallPanel(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_HOLYCALL_PANEL, "hh", baseType, subType);

        /// <summary>33222 稀有抽(read: BaseType,SubType,无次数参数——服务端固定消耗 RareDrawTimes)。</summary>
        public void RequestHolyCallRareDraw(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_HOLYCALL_RARE_DRAW, "hh", baseType, subType);
    }
}
