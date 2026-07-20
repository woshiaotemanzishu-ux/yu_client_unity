using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Banquet
{
    /// <summary>
    /// 婚宴(婚礼)控制器(对标老客户端 BanquetController,模块 172)。图标壳(17249/17256)+ 轮24 PB
    /// 扩数据层(17250-17298 等 22 个活号,扩壳不重建)。
    /// 进游戏请求 17249(婚礼状态)与 17256(婚礼召集),回包据条件增删图标:
    ///   17249 now_wedding_state==2 → AddIcon(172@2);上次==2 且这次==0 → DeleteIcon(172@2)(对标老端 On17249 的 tri-state)。
    ///   17256 type==1 且 wedding_list 非空 → AddIcon(172@1);否则 DeleteIcon(172@1)(对标老端 SetBanquetCall)。
    /// 老端 172@1 的触发在 SetBanquetCall(由 17256 驱动),原逻辑还按 start_time 延时上图标、按主角是否新人切文案,
    /// 那些属玩法呈现;图标层面等价于「婚礼活动开着就显示」,故本期简化为随激活态即时增删。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求(172@1 配置 open_lv=130,升到 130 级后图标才能过 AddIcon 的门),
    /// 走 _lastLevel 去抖只在等级真变时重发(⚠自查:复核老端 BanquetController.ts 全文,并无 CHANGE_LEVEL
    /// 监听——本机制是既有前序轮次的防御性补充,非逐字对标,本轮"扩壳不重建"不动它;RequestStartup 新增的
    /// 17250 请求也会随之被这套去抖顺带复发,比老端多请求一次,无害)。
    ///
    /// 轮24 PB 数据层扩展纪律:①服务端门禁(NowWeddingState==2/AskInviteLv=130/婚礼场景 ?WeddingScene)
    /// 均只服务端强制,本端不做客户端预检(无 UI/无婚礼场景可供预检),发送方法照建、门禁写注释存档,
    /// 调用时机留给 UI/场景轮接线;②裁决4:BanquetModel 自持 now_wedding_state 等字段,不跨 Model 读写
    /// MarriageModel(见 BanquetModel 类注释);③17254/17255(邀请应答旧机制)/17269/17274(捣蛋鬼)/
    /// 17273(老端空函数体)/17280-17294(宝宝形象死协议壳)——killlist 严禁触碰,不在本控制器注册。
    /// </summary>
    public sealed class BanquetController : BaseController
    {
        public static readonly BanquetController Instance = new BanquetController();
        private BanquetController() { }

        public const string ICON_TYPE_WEDDING = BanquetModel.ICON_TYPE_WEDDING;
        public const string ICON_TYPE_GUEST = BanquetModel.ICON_TYPE_GUEST;
        public const int CANDIES_TYPE_NORMAL = 1;
        public const int CANDIES_TYPE_SPECIAL = 2;

        // 复请求的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.BANQUET_WEDDING_STATE, On17249);
            RegisterProtocal(Proto.BANQUET_CALL, On17256);

            // ---- 轮24 PB 数据层扩展(22 个活号,逐条对标 pt_172.erl/BanquetController.ts) ----
            RegisterProtocal(Proto.BANQUET_APPLY_INFO, On17250);
            RegisterProtocal(Proto.BANQUET_APPLY_SEND, On17251);
            RegisterProtocal(Proto.BANQUET_INVITE_INFO, On17252);
            RegisterProtocal(Proto.BANQUET_INVITE_SEND, On17253);
            RegisterProtocal(Proto.BANQUET_ASK_INVITE, On17257);
            RegisterProtocal(Proto.BANQUET_BUY_INVITE_CARD, On17258);
            RegisterProtocal(Proto.BANQUET_BUY_INVITE_MAX, On17259);
            RegisterProtocal(Proto.BANQUET_OPEN_ASK_INVITE, On17260);
            RegisterProtocal(Proto.BANQUET_ANSWER_ASK_INVITE, On17261);
            RegisterProtocal(Proto.BANQUET_SCENE_ANIME_INFO, On17262);
            RegisterProtocal(Proto.BANQUET_WEDDING_INFO, On17265);
            RegisterProtocal(Proto.BANQUET_SPRINKLE_CANDIES, On17266);
            RegisterProtocal(Proto.BANQUET_SET_OFF_FIRES, On17267);
            RegisterProtocal(Proto.BANQUET_SEND_DANMU, On17270);
            RegisterProtocal(Proto.BANQUET_COLLECT_RESULT, On17271);
            RegisterProtocal(Proto.BANQUET_GOODS_INFO, On17272);
            RegisterProtocal(Proto.BANQUET_EXP_INFO, On17275);
            RegisterProtocal(Proto.BANQUET_WEDDING_START_PUSH, On17276);
            RegisterProtocal(Proto.BANQUET_AURA_PUSH, On17277);
            RegisterProtocal(Proto.BANQUET_AURA_REWARD_PUSH, On17278);
            RegisterProtocal(Proto.BANQUET_TABLE_REWARD_PUSH, On17279);
            RegisterProtocal(Proto.BANQUET_ONE_INVITE, On17298);

            // 对标老端 CHANGE_LEVEL 复算图标:等级变化时复请求(172@1 需 130 级)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_WEDDING);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_GUEST);
            BanquetModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START:
        /// InitCfg()+deleteIcon(172@2/172@1)+SendFmtToGame(17249/17256/17250))。GameStartController.cs
        /// 不在本包所有权范围,不新增专属 GAME_START 钩子,统一走这个既有单一入口(该方法本身也被
        /// OnRoleInfoUpdate 的等级去抖复调用,17250 因此比老端多发一次,无害,见类注释)。</summary>
        public void RequestStartup()
        {
            _ = BanquetConfigs.EnsureLoaded(); // 对标老端 banquetModel.InitCfg()(fire-and-forget,不阻塞下面的请求)
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_GUEST);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_WEDDING);
            // read(17249,_)->{ok,[]} / read(17256,_)->{ok,[]} / read(17250,_)->{ok,[]}:请求均无字段,裸发。
            SendFmt(Proto.BANQUET_WEDDING_STATE);
            SendFmt(Proto.BANQUET_CALL);
            SendFmt(Proto.BANQUET_APPLY_INFO); // 轮24 PB 新增:预约/报名视图数据(原壳注释"图标不需不发"已过期)
        }

        // 17249: now_wedding_state:c, begin_time:i(对标老端 On17249)
        private void On17249(NetReader r)
        {
            int nowState = r.ReadU8();
            int beginTime = (int)r.ReadU32();

            BanquetModel m = BanquetModel.Instance;
            // 对标老端 On17249:state==2 加 172@2;上次==2 且这次==0 才删;其余状态图标不动。
            if (nowState == 2) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE_GUEST);
            else if (m.BanqState == 2 && nowState == 0) ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_GUEST);

            m.NowWeddingState = nowState;
            m.BeginTime = beginTime;
            m.BanqState = nowState; // 记录本次状态供下次 tri-state 判定(对标 banquetModel.banqState = scmd.now_wedding_state)

            GameLog.Info("Banquet", "17249 婚礼状态: now_wedding_state={0} begin_time={1} guestOpen={2}",
                nowState, beginTime, m.GetGuestIconOpen());
        }

        // 17256: type:c, wedding_list[u16 × item_to_bin_24]
        //   item_to_bin_24 = { wedding_type:c, start_time:i,
        //     man_list[u16 × role], woman_list[u16 × role], guest_list[u16 × { role_id:l }] }
        // 图标只看 type==1 且 wedding_list 非空(对标老端 SetBanquetCall);整包仍按字节吃干净。
        private void On17256(NetReader r)
        {
            int type = r.ReadU8();
            int weddingCount = r.ReadU16();
            for (int i = 0; i < weddingCount; i++)
            {
                r.ReadU8();          // wedding_type
                r.ReadU32();         // start_time(老端算倒计时文本用,本期图标只做显隐)
                ReadRoleList(r);     // man_list
                ReadRoleList(r);     // woman_list
                int guestCount = r.ReadU16();
                for (int g = 0; g < guestCount; g++) r.ReadU64(); // guest role_id
            }

            BanquetModel m = BanquetModel.Instance;
            m.WeddingActive = (type == 1 && weddingCount > 0);
            if (m.GetWeddingIconOpen()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE_WEDDING);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_WEDDING);

            GameLog.Info("Banquet", "17256 婚礼召集: type={0} weddings={1} weddingOpen={2}",
                type, weddingCount, m.GetWeddingIconOpen());
        }

        // item_to_bin_25/26(man_list/woman_list 元素一致):
        //   role_id:l, name:s, lv:h, combat_power:l, sex:c, vip:i, career:c, turn:c, picture:s, picture_ver:i
        // 图标不需要这些字段,仅按字节顺序读掉以对齐包体。
        private static void ReadRoleList(NetReader r)
        {
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                r.ReadU64();    // role_id
                r.ReadString(); // name
                r.ReadU16();    // lv
                r.ReadU64();    // combat_power
                r.ReadU8();     // sex
                r.ReadU32();    // vip
                r.ReadU8();     // career
                r.ReadU8();     // turn
                r.ReadString(); // picture
                r.ReadU32();    // picture_ver
            }
        }

        // 对标老端 CHANGE_LEVEL:主角等级变化复请求(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        // =====================================================================================
        // 轮24 PB 数据层扩展(17250-17298,共 22 个活号;wire 逐条核对 pt_172.erl 原文/触发链核对
        // BanquetController.ts 原文,不抄侦察稿)。
        // =====================================================================================

        private static void ShowError(int code) => TipsManager.Toast("错误(" + code + ")"); // 错误码表未移植,显码降级

        // ---- 17250 预约/报名视图数据 ----

        /// <summary>17250 预约/报名视图数据(C2S 空包,已并入 RequestStartup 恒发;保留独立方法供 UI/场景轮
        /// 主动重拉用)。</summary>
        public void RequestApplyInfo() => SendFmt(Proto.BANQUET_APPLY_INFO);

        // 17250: code:i, now_wedding_state:c, my_wedding_times[u16×{wedding_type:c,use_times:h,max_times:h,
        //   order_today:c}], day_list[u16×{order_unix_date:i, time_list[u16×{time_id:c,
        //   order_list[u16×{role_id_m:l,role_id_w:l,wedding_type:c,if_own:c}]}]}](三层嵌套)。
        private void On17250(NetReader r)
        {
            int code = r.ReadI32();
            int nowWeddingState = r.ReadU8();
            List<BanquetModel.WeddingTimesEntry> myWeddingTimes = r.ReadArray(ReadWeddingTimesEntry);
            List<BanquetModel.DayEntry> dayList = r.ReadArray(ReadDayEntry);
            if (code == 1)
            {
                var info = new BanquetModel.ApplyViewInfo { NowWeddingState = nowWeddingState };
                info.MyWeddingTimes.AddRange(myWeddingTimes);
                info.DayList.AddRange(dayList);
                BanquetModel.Instance.SetApplyViewData(info);
                EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_APPLY_INFO_UPDATE);
            }
            else if (code != 1720012) // 老端特例:该码不显码(本代理原文核实=err172_couple_single,data_error_code.erl:3034)
            {
                ShowError(code);
            }
            GameLog.Info("Banquet", "17250 预约视图数据 code={0} nowWeddingState={1} times={2} days={3} canApply={4}",
                code, nowWeddingState, myWeddingTimes.Count, dayList.Count, BanquetModel.Instance.CanApply);
        }

        private static BanquetModel.WeddingTimesEntry ReadWeddingTimesEntry(NetReader r) => new BanquetModel.WeddingTimesEntry
        {
            WeddingType = r.ReadU8(), UseTimes = r.ReadU16(), MaxTimes = r.ReadU16(), OrderToday = r.ReadU8() != 0,
        };

        private static BanquetModel.DayEntry ReadDayEntry(NetReader r)
        {
            var e = new BanquetModel.DayEntry { OrderUnixDate = r.ReadU32() };
            e.TimeList.AddRange(r.ReadArray(ReadTimeSlotEntry));
            return e;
        }

        private static BanquetModel.TimeSlotEntry ReadTimeSlotEntry(NetReader r)
        {
            var e = new BanquetModel.TimeSlotEntry { TimeId = r.ReadU8() };
            e.OrderList.AddRange(r.ReadArray(ReadDayOrderEntry));
            return e;
        }

        private static BanquetModel.DayOrderEntry ReadDayOrderEntry(NetReader r) => new BanquetModel.DayOrderEntry
        {
            RoleIdM = r.ReadU64(), RoleIdW = r.ReadU64(), WeddingType = r.ReadU8(), IfOwn = r.ReadU8() != 0,
        };

        // ---- 17251 预约婚礼 ----

        /// <summary>预约婚礼(C2S "ccc" day_id,time_id,wedding_type;服务端多重校验:MarriageType/
        /// WeddingPid/NowWeddingState==3/DayId超限/时段配置/当日次数等,逐分支各自回错误码,详见
        /// pp_marriage.erl:1652-1743)。</summary>
        public void RequestApply(int dayId, int timeId, int weddingType) =>
            SendFmt(Proto.BANQUET_APPLY_SEND, "ccc", dayId, timeId, weddingType);

        // 17251: code:i, time:i, wedding_type:c, man_list[u16×bin_16], woman_list[u16×bin_17]
        //   bin_16/17: role_id:l,name:s,lv:h,combat_power:l,sex:c,vip:i,career:c,turn:c(**无 picture,
        //   勿与 17256 的 item_to_bin_25/26 混淆**)。老端也不消费 man_list/woman_list 具体内容(仅用于
        //   服务端两侧各自的成功回执模板),本端同样只解析保游标、不落地。
        private void On17251(NetReader r)
        {
            int code = r.ReadI32();
            long time = r.ReadU32();
            int weddingType = r.ReadU8();
            List<int> manList = r.ReadArray(SkipWeddingPartyEntry);
            List<int> womanList = r.ReadArray(SkipWeddingPartyEntry);
            bool success = code == 1 || code == 1720034; // 1720034=err172_wedding_order_success(配偶侧回执)
            if (success)
            {
                SendFmt(Proto.BANQUET_WEDDING_STATE); // 17249
                SendFmt(Proto.BANQUET_APPLY_INFO);    // 17250
                // 老端额外 Fire(OPEN_VIEW,"BanquetNoticeView",2)——UI 层,本轮不接,留 TODO(场景/UI 轮)。
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_APPLY_RESULT, success);
            GameLog.Info("Banquet", "17251 预约婚礼 code={0} time={1} weddingType={2} man={3} woman={4} success={5}",
                code, time, weddingType, manList.Count, womanList.Count, success);
        }

        /// <summary>bin_16/17(role_id:l,name:s,lv:h,combat_power:l,sex:c,vip:i,career:c,turn:c)按字节顺序
        /// 读掉,不落地(老端也不消费,见 On17251 注释)。返回值仅为满足 ReadArray&lt;T&gt; 泛型签名占位,
        /// 调用方只取 Count 判断数组长度,不作实际语义使用。</summary>
        private static int SkipWeddingPartyEntry(NetReader r)
        {
            r.ReadU64(); r.ReadString(); r.ReadU16(); r.ReadU64(); r.ReadU8(); r.ReadU32(); r.ReadU8(); r.ReadU8();
            return 0;
        }

        // ---- 17252 婚宴邀请视图数据 ----

        /// <summary>邀请视图数据(C2S 空包)。服务端 NowWeddingState==2 才放行(pp_marriage.erl:1755),
        /// 否则回错误壳(字段占位全 0/"")。</summary>
        public void RequestInviteInfo() => SendFmt(Proto.BANQUET_INVITE_INFO);

        // 17252: code:i, my_role_id:l, my_name:s, my_picture:s, my_picture_ver:i, lover_role_id:l,
        //   lover_name:s, lover_picture:s, lover_picture_ver:i, wedding_type:c, wedding_time:i,
        //   if_order_again:c, less_invite_num:c, guest_num:c, guest_list[u16×{role_id:l,answer_type:c,
        //   name:s}], ask_invite_list[u16×{role_id:l,name:s}](**无 answer_type,与 guest_list 形状不同**)。
        private void On17252(NetReader r)
        {
            int code = r.ReadI32();
            long myRoleId = r.ReadU64();
            string myName = r.ReadString();
            string myPicture = r.ReadString();
            long myPictureVer = r.ReadU32();
            long loverRoleId = r.ReadU64();
            string loverName = r.ReadString();
            string loverPicture = r.ReadString();
            long loverPictureVer = r.ReadU32();
            int weddingType = r.ReadU8();
            long weddingTime = r.ReadU32();
            bool ifOrderAgain = r.ReadU8() != 0;
            int lessInviteNum = r.ReadU8();
            int guestNum = r.ReadU8();
            List<BanquetModel.GuestEntry> guestList = r.ReadArray(ReadGuestEntry);
            List<BanquetModel.AskEntry> askInviteList = r.ReadArray(ReadAskInviteListEntry);
            if (code != 1)
            {
                ShowError(code);
                GameLog.Info("Banquet", "17252 邀请视图数据 code={0}(失败,字段占位)", code);
                return;
            }
            var info = new BanquetModel.InviteViewInfo
            {
                MyRoleId = myRoleId, MyName = myName, MyPicture = myPicture, MyPictureVer = myPictureVer,
                LoverRoleId = loverRoleId, LoverName = loverName, LoverPicture = loverPicture, LoverPictureVer = loverPictureVer,
                WeddingType = weddingType, WeddingTime = weddingTime, IfOrderAgain = ifOrderAgain,
                LessInviteNum = lessInviteNum, GuestNum = guestNum,
            };
            info.GuestList.AddRange(guestList);
            info.AskInviteList.AddRange(askInviteList);
            BanquetModel.Instance.SetInviteViewData(info);
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_INVITE_INFO_UPDATE);
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_GUEST_LIST_UPDATE);
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_ASK_DATA_UPDATE, BanquetModel.Instance.NewApply);
            GameLog.Info("Banquet", "17252 邀请视图数据 code={0} guestNum={1} guestList={2} askList={3}",
                code, guestNum, guestList.Count, askInviteList.Count);
        }

        private static BanquetModel.GuestEntry ReadGuestEntry(NetReader r) => new BanquetModel.GuestEntry
        {
            RoleId = r.ReadU64(), AnswerType = r.ReadU8(), Name = r.ReadString(),
        };

        private static BanquetModel.AskEntry ReadAskInviteListEntry(NetReader r) => new BanquetModel.AskEntry
        {
            RoleId = r.ReadU64(), Name = r.ReadString(), AnswerType = -1, // 17252 ask_invite_list 无 answer_type 字段
        };

        // ---- 17253 邀请宾客 ----

        /// <summary>邀请宾客(C2S "h"+N×"l" count,role_id...;对标老端单条邀请 WriteBegin(17253)+
        /// WriteFMT("h",1)+WriteFMT("l",roleId) 的通用化——本端支持批量传入)。服务端校验:不可邀请自己/
        /// 不可邀请配偶/NowWeddingState==2/每个被邀请人 Lv&gt;=130(err172_marriage_ask_lv_limit,
        /// pp_marriage.erl:1788),本端不预检。</summary>
        public void RequestInviteGuests(IReadOnlyList<long> roleIds)
        {
            var fmt = new StringBuilder("h");
            var args = new List<object> { roleIds?.Count ?? 0 };
            if (roleIds != null)
            {
                foreach (long id in roleIds) { fmt.Append('l'); args.Add(id); }
            }
            SendFmt(Proto.BANQUET_INVITE_SEND, fmt.ToString(), args.ToArray());
        }

        // 17253: code:i, invite_list[u16×role_id:l](**纯 RoleId,无 Name/Type 包装,与 17252/17260 的
        //   {RoleId,AnswerType,Name} 形状不同,勿混淆**)。
        private void On17253(NetReader r)
        {
            int code = r.ReadI32();
            List<long> inviteList = r.ReadArray(rr => (long)rr.ReadU64());
            bool success = code == 1 || code == 1720033; // 1720033=err172_wedding_invite_success(data_error_code.erl:3118),原样镜像成功码集合
            if (success) SendFmt(Proto.BANQUET_INVITE_INFO); // 重发 17252
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_INVITE_SEND_RESULT, success);
            GameLog.Info("Banquet", "17253 邀请宾客 code={0} count={1} success={2}", code, inviteList.Count, success);
        }

        // ---- 17257/17258 索要请柬 / 购买请柬(买路进场) ----

        /// <summary>索要请柬(C2S "l" role_id_m,对标老端 BanquetNoticeView "索要请柬"点击)。服务端
        /// Lv&gt;=130 门(AskInviteLv,pp_marriage.erl:1843 err172_marriage_ask_lv_limit),本端不预检。</summary>
        public void RequestAskInvite(long roleIdM) => SendFmt(Proto.BANQUET_ASK_INVITE, "l", roleIdM);

        private void On17257(NetReader r)
        {
            int code = r.ReadI32();
            bool success = code == 1;
            if (success) TipsManager.Toast("索要成功");
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_ASK_INVITE_RESULT, success);
            GameLog.Info("Banquet", "17257 索要请柬 code={0}", code);
        }

        /// <summary>购买请柬/买路进场(C2S "l" role_id_m,对标老端 BanquetNoticeView "buyBtn"点击)。服务端
        /// 同 17257 Lv&gt;=130 门(pp_marriage.erl:1860,本代理原文核实补全,侦察稿未提及)+
        /// ?WeddingGuestMaxNumPrice 消耗校验,本端不预检。</summary>
        public void RequestBuyInviteCard(long roleIdM) => SendFmt(Proto.BANQUET_BUY_INVITE_CARD, "l", roleIdM);

        private void On17258(NetReader r)
        {
            int code = r.ReadI32();
            long roleIdM = r.ReadU64();
            bool success = code == 1;
            if (success) TipsManager.Toast("购买成功");
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_BUY_INVITE_CARD_RESULT, success);
            GameLog.Info("Banquet", "17258 购买请柬 code={0} roleIdM={1}", code, roleIdM);
        }

        // ---- 17259 购买邀请名额上限 ----

        /// <summary>购买邀请名额上限(C2S "c" buy_num,buy_num&gt;0)。服务端 NowWeddingState==2 门。</summary>
        public void RequestBuyInviteMax(int buyNum) => SendFmt(Proto.BANQUET_BUY_INVITE_MAX, "c", buyNum);

        private void On17259(NetReader r)
        {
            int code = r.ReadI32();
            int lessInviteNum = r.ReadU8();
            int guestNum = r.ReadU8();
            bool success = code == 1;
            if (success)
            {
                SendFmt(Proto.BANQUET_INVITE_INFO); // 重发 17252
            }
            else if (code != 1720036) // 1720036=err172_wedding_buy_max_num_success(data_error_code.erl:3130,虽已是成功语义
                                       // 但老端只在 code==1 分支触发 SendFmtToGame(17252)+Fire(BUY_MAX),此码仅排除显码)
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_BUY_MAX_RESULT, success);
            GameLog.Info("Banquet", "17259 购买邀请名额上限 code={0} lessInviteNum={1} guestNum={2}", code, lessInviteNum, guestNum);
        }

        // ---- 17260 打开索要/邀请列表(双 type 分流) ----

        /// <summary>打开索要/邀请列表(C2S "h"+N×"c" count,type...;老端固定传 [2]。type:1=索要请柬列表/
        /// 2=宾客邀请列表)。服务端 NowWeddingState==2 门。</summary>
        public void RequestOpenAskInvite(IReadOnlyList<int> types)
        {
            var fmt = new StringBuilder("h");
            var args = new List<object> { types?.Count ?? 0 };
            if (types != null)
            {
                foreach (int t in types) { fmt.Append('c'); args.Add(t); }
            }
            SendFmt(Proto.BANQUET_OPEN_ASK_INVITE, fmt.ToString(), args.ToArray());
        }

        // 17260: code:i, less_invite_num:c, list[u16×{type:c, info_list[u16×{role_id:l,answer_type:c,name:s}]}]
        //   **双 type 分流**(对标老端 On17260):type==1→AskData(按"本次数量是否比上次更多"判定 172@2
        //   红点是否算"新申请");type==2→GuestList(与 17252 的 guest_list 共用同一顶层桶,形状相同)。
        private void On17260(NetReader r)
        {
            int code = r.ReadI32();
            int lessInviteNum = r.ReadU8();
            List<(int type, List<BanquetModel.AskEntry> infoList)> groups = r.ReadArray(ReadOpenAskGroup);
            if (code != 1)
            {
                ShowError(code);
                GameLog.Info("Banquet", "17260 打开索要/邀请列表 code={0}(失败)", code);
                return;
            }
            BanquetModel m = BanquetModel.Instance;
            m.LessInviteNum = lessInviteNum;
            foreach ((int type, List<BanquetModel.AskEntry> infoList) group in groups)
            {
                if (group.type == 1)
                {
                    bool wasEmpty = !m.HasAskData;
                    int prevCount = m.AskData?.Count ?? 0;
                    bool isNew = (wasEmpty && group.infoList.Count > 0) || (!wasEmpty && group.infoList.Count > prevCount);
                    m.AskData = group.infoList;
                    m.NewApply = isNew;
                    EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_ASK_DATA_UPDATE, isNew);
                }
                else if (group.type == 2)
                {
                    m.GuestList.Clear();
                    foreach (BanquetModel.AskEntry e in group.infoList)
                    {
                        m.GuestList.Add(new BanquetModel.GuestEntry { RoleId = e.RoleId, AnswerType = e.AnswerType, Name = e.Name });
                    }
                    EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_GUEST_LIST_UPDATE);
                }
            }
            GameLog.Info("Banquet", "17260 打开索要/邀请列表 code={0} lessInviteNum={1} groups={2}", code, lessInviteNum, groups.Count);
        }

        private static (int type, List<BanquetModel.AskEntry> infoList) ReadOpenAskGroup(NetReader r)
        {
            int type = r.ReadU8();
            List<BanquetModel.AskEntry> infoList = r.ReadArray(ReadOpenAskEntry);
            return (type, infoList);
        }

        private static BanquetModel.AskEntry ReadOpenAskEntry(NetReader r) => new BanquetModel.AskEntry
        {
            RoleId = r.ReadU64(), AnswerType = r.ReadU8(), Name = r.ReadString(),
        };

        // ---- 17261 回应索要请柬 ----

        /// <summary>回应索要请柬(C2S "h"+N×"lc" count,(role_id,answer_type)...)。服务端要求
        /// wedding_pid 存活(err172_wedding_not_start)。</summary>
        public void RequestAnswerAskInvite(IReadOnlyList<(long roleId, int answerType)> list)
        {
            var fmt = new StringBuilder("h");
            var args = new List<object> { list?.Count ?? 0 };
            if (list != null)
            {
                foreach ((long roleId, int answerType) e in list) { fmt.Append("lc"); args.Add(e.roleId); args.Add(e.answerType); }
            }
            SendFmt(Proto.BANQUET_ANSWER_ASK_INVITE, fmt.ToString(), args.ToArray());
        }

        private void On17261(NetReader r)
        {
            int code = r.ReadI32();
            bool success = code == 1;
            if (!success) ShowError(code);
            SendFmt(Proto.BANQUET_INVITE_INFO); // 无条件重发 17252(对标老端 On17261:先判code显码,再无条件重发)
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_ANSWER_ASK_RESULT, success);
            GameLog.Info("Banquet", "17261 回应索要请柬 code={0}", code);
        }

        // ---- 17262 婚礼动画场景信息(需在婚礼场景) ----

        /// <summary>婚礼动画场景信息(C2S 空包)。服务端要求 SceneId==?WeddingScene 且对方 wedding_pid 存活,
        /// 否则回错误壳(err172_wedding_not_start/err172_wedding_not_scene)。Unity 无婚礼场景,场景门禁
        /// 本端不预检,调用留 UI/场景轮。</summary>
        public void RequestSceneAnimeInfo() => SendFmt(Proto.BANQUET_SCENE_ANIME_INFO);

        /// <summary>进入婚礼场景(C2S 17263 "l":role_id_m)。老端 BanquetNoticeView 以婚礼男方
        /// role_id 作为婚礼实例入口参数；S2C 错误出口仍归 MarriageController 注册，Banquet 不重复收包。</summary>
        public void RequestEnterWeddingScene(long roleIdM)
        {
            (string fmt, object[] args) = BuildEnterWeddingScenePayload(roleIdM);
            SendFmt(Proto.MARRIAGE_BANQUET_ENTER_SCENE, fmt, args);
        }

        /// <summary>17263 请求编码纯函数，供 CliVerify 对真实 BIG_ENDIAN 出站字节做断言。</summary>
        private static (string fmt, object[] args) BuildEnterWeddingScenePayload(long roleIdM) =>
            ("l", new object[] { roleIdM });

        /// <summary>离开婚礼场景(C2S 17264 空包)。S2C 错误出口仍归 MarriageController。</summary>
        public void RequestLeaveWeddingScene()
        {
            (string fmt, object[] args) = BuildLeaveWeddingScenePayload();
            SendFmt(Proto.MARRIAGE_BANQUET_LEAVE_SCENE, fmt, args);
        }

        /// <summary>17264 请求编码纯函数，供 CliVerify 断言空载荷。</summary>
        private static (string fmt, object[] args) BuildLeaveWeddingScenePayload() =>
            (null, new object[0]);

        // 17262: code:i, man_list[u16×{role_id_m:l,figure_m:FigureProto}], woman_list[u16×{role_id_w:l,
        //   figure_w:FigureProto}], guest_position_list[u16×{pos_id:c,guest_role_id:l,if_enter:c}]。
        private void On17262(NetReader r)
        {
            int code = r.ReadI32();
            List<BanquetModel.ScenePersonEntry> manList = r.ReadArray(ReadScenePersonEntry);
            List<BanquetModel.ScenePersonEntry> womanList = r.ReadArray(ReadScenePersonEntry);
            List<BanquetModel.GuestPositionEntry> guestPositionList = r.ReadArray(ReadGuestPositionEntry);
            if (code != 1)
            {
                ShowError(code);
                GameLog.Info("Banquet", "17262 婚礼动画场景信息 code={0}(失败)", code);
                return;
            }
            var info = new BanquetModel.WeddingRoleListInfo();
            info.ManList.AddRange(manList);
            info.WomanList.AddRange(womanList);
            info.GuestPositionList.AddRange(guestPositionList);
            BanquetModel.Instance.WeddingRoleList = info;
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_SCENE_ANIME_UPDATE);
            GameLog.Info("Banquet", "17262 婚礼动画场景信息 code={0} man={1} woman={2} guestPos={3}",
                code, manList.Count, womanList.Count, guestPositionList.Count);
        }

        private static BanquetModel.ScenePersonEntry ReadScenePersonEntry(NetReader r) => new BanquetModel.ScenePersonEntry
        {
            RoleId = r.ReadU64(), Figure = FigureProto.Read(r),
        };

        private static BanquetModel.GuestPositionEntry ReadGuestPositionEntry(NetReader r) => new BanquetModel.GuestPositionEntry
        {
            PosId = r.ReadU8(), GuestRoleId = r.ReadU64(), IfEnter = r.ReadU8() != 0,
        };

        // ---- 17265 婚礼信息(需在婚礼场景) ----

        /// <summary>婚礼信息(C2S 空包)。服务端要求 SceneId==?WeddingScene,否则回错误壳(字段占位全 0)。
        /// Unity 无婚礼场景,场景门禁本端不预检,调用留 UI/场景轮。</summary>
        public void RequestWeddingInfo() => SendFmt(Proto.BANQUET_WEDDING_INFO);

        private void On17265(NetReader r)
        {
            int code = r.ReadI32();
            int stageId = r.ReadU8();
            long stageEndTime = r.ReadU32();
            long aura = r.ReadU32();
            long lessNormalCandies = r.ReadU32();
            long lessSpecialCandies = r.ReadU32();
            int guestsNum = r.ReadU8();
            if (code == 1)
            {
                BanquetModel.Instance.BanquetData = new BanquetModel.WeddingSceneInfo
                {
                    StageId = stageId, StageEndTime = stageEndTime, Aura = aura,
                    LessNormalCandies = lessNormalCandies, LessSpecialCandies = lessSpecialCandies, GuestsNum = guestsNum,
                };
                EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_INFO_UPDATE);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Banquet", "17265 婚礼信息 code={0} stageId={1} aura={2} guestsNum={3}",
                code, stageId, aura, guestsNum);
        }

        // ---- 17266 撒喜糖 / 17267 放烟花(均需在婚礼场景,均场景内玩法号) ----

        /// <summary>撒喜糖(C2S "c" candies_type;1=普通喜糖(配置物品 8002003),2=特殊喜糖
        /// (配置物品 8002004))。服务端要求婚礼场景+wedding_pid 存活+RoleIdM∈{自己,配偶}。</summary>
        public void RequestSprinkleCandies(int candiesType)
        {
            if (candiesType != CANDIES_TYPE_NORMAL && candiesType != CANDIES_TYPE_SPECIAL)
            {
                GameLog.Warn("Banquet", "RequestSprinkleCandies 非法 candies_type={0},只允许 1(普通)/2(特殊)", candiesType);
                return;
            }
            (string fmt, object[] args) = BuildSprinkleCandiesPayload(candiesType);
            SendFmt(Proto.BANQUET_SPRINKLE_CANDIES, fmt, args);
        }

        /// <summary>17266 请求编码纯函数，供 CliVerify 锁定 type=1/2 而非物品 id。</summary>
        private static (string fmt, object[] args) BuildSprinkleCandiesPayload(int candiesType) =>
            ("c", new object[] { candiesType });

        private void On17266(NetReader r)
        {
            int code = r.ReadI32();
            bool success = code == 1;
            if (success) SendFmt(Proto.BANQUET_GOODS_INFO); // 重发 17272
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_CANDIES_RESULT, success);
            GameLog.Info("Banquet", "17266 撒喜糖 code={0} success={1}", code, success);
        }

        /// <summary>放烟花(C2S "c" fires_type)。服务端要求婚礼场景+wedding_pid 存活+config_wedding_fires
        /// 命中+可发奖校验。</summary>
        public void RequestSetOffFires(int firesType) => SendFmt(Proto.BANQUET_SET_OFF_FIRES, "c", firesType);

        /// <summary>对标老端 On17267:先取 config_wedding_fires 缓存判空(this.wedding_fires_cfg 整体 if 守卫)——
        /// **配置未载时老端整段跳过,不重发 17272、不显码**,本端镜像该门禁(BanquetConfigs.EnsureLoaded 已在
        /// RequestStartup 触发,进游戏后应已就绪;若因时序未就绪则本次推送静默丢弃,同老端行为)。
        /// 配置已载:按 fires_type 查行播场景特效(charact 字段,老端 PlayFlowerEffectByName)——Unity 无婚礼
        /// 场景,该分支留 TODO;无条件重发 17272;仅当 role_id 等于本端主角时才 ErrorCodeShow(即只有自己
        /// 触发的这次请求才提示,别人放烟花的广播不提示)。</summary>
        private void On17267(NetReader r)
        {
            int code = r.ReadI32();
            r.ReadString(); // error_code_args:仅服务端拼错误提示用,本端错误码降级展示不需要,读掉保游标
            int firesType = r.ReadU8();
            long roleId = r.ReadU64();
            if (!BanquetConfigs.IsLoaded)
            {
                GameLog.Info("Banquet", "17267 放烟花结果 config_wedding_fires 未载,老端整段跳过 code={0} firesType={1}", code, firesType);
                return;
            }
            BanquetConfigs.FiresRow fires = BanquetConfigs.GetFires(firesType);
            _ = fires; // TODO(场景轮):fires!=null 时老端播 fires.Charact 场景特效,Unity 无婚礼场景不接。
            SendFmt(Proto.BANQUET_GOODS_INFO); // 无条件重发 17272
            // 1720071=err172_wedding_fires_success：服务端婚礼进程广播烟花成功时使用的业务成功码。
            bool success = code == 1 || code == 1720071;
            if (roleId == RoleModel.Instance.RoleId && !success) ShowError(code); // 仅本端角色触发时才显码
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_FIRES_RESULT, success);
            GameLog.Info("Banquet", "17267 放烟花结果 code={0} firesType={1} roleId={2}", code, firesType, roleId);
        }

        // ---- 17270 发弹幕(需在婚礼场景) ----

        /// <summary>发弹幕(C2S "si" msg,tk_time;老端 tk_time 固定传 0)。服务端内部转发
        /// pp_chat:handle(11001,场景频道)。</summary>
        public void RequestSendDanmu(string msg, long tkTime = 0) => SendFmt(Proto.BANQUET_SEND_DANMU, "si", msg, tkTime);

        private void On17270(NetReader r)
        {
            int code = r.ReadI32();
            bool success = code == 1;
            // 对标老端 On17270:仅 code==1 时 Fire(BARRIAGE_SUCCESS),失败无 ShowError、无其它分支。
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_DANMU_RESULT, success);
            GameLog.Info("Banquet", "17270 发弹幕 code={0}", code);
        }

        // ---- 17271 吃桌菜/采集喜糖结果推送(无 read,S2C only) ----

        private void On17271(NetReader r)
        {
            int code = r.ReadI32();
            r.ReadString(); // error_code_args:仅服务端拼错误提示用,读掉保游标
            int type = r.ReadU8();
            if (type == 1) SendFmt(Proto.BANQUET_GOODS_INFO); // 桌菜"喜宴"成功时重发 17272
            bool success = code == 1;
            if (!success) ShowError(code);
            else if (type == 1) TipsManager.Toast("获得喜宴");
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_COLLECT_RESULT, type, success);
            GameLog.Info("Banquet", "17271 吃桌菜/采集喜糖结果 code={0} type={1}", code, type);
        }

        // ---- 17272 婚礼道具使用信息(需在婚礼场景) ----

        /// <summary>婚礼道具使用信息/桌菜采集状态(C2S 空包)。服务端要求婚礼场景,否则回错误壳。</summary>
        public void RequestGoodsInfo() => SendFmt(Proto.BANQUET_GOODS_INFO);

        private void On17272(NetReader r)
        {
            int code = r.ReadI32();
            bool ifMaster = r.ReadU8() != 0;
            int freeCandies = r.ReadU8();
            int freeFires = r.ReadU8();
            List<long> collectTableList = r.ReadArray(rr => (long)rr.ReadU32());
            if (code != 1)
            {
                ShowError(code);
                GameLog.Info("Banquet", "17272 婚礼道具使用信息 code={0}(失败)", code);
                return;
            }
            var info = new BanquetModel.GoodsInfoData { IfMaster = ifMaster, FreeCandies = freeCandies, FreeFires = freeFires };
            info.CollectTableList.AddRange(collectTableList);
            BanquetModel.Instance.ApplyGoodsInfo(info);
            // TODO(场景轮):老端 UpdateDesk() 额外按 Scene.GetClickTarget() 清除场景内已采集目标高亮,
            // Unity 无婚礼场景,不接。
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_GOODS_INFO_UPDATE);
            GameLog.Info("Banquet", "17272 婚礼道具使用信息 code={0} freeCandies={1} freeFires={2} tableCount={3}",
                code, freeCandies, freeFires, collectTableList.Count);
        }

        // ---- 17275 婚礼获得总经验(需在婚礼场景) ----

        /// <summary>婚礼获得总经验(C2S 空包)。服务端要求婚礼场景,否则静默 skip(不回包)。</summary>
        public void RequestExpInfo() => SendFmt(Proto.BANQUET_EXP_INFO);

        // 17275: all_exp:l(**无 Code 前缀,唯一字段**)。
        private void On17275(NetReader r)
        {
            long allExp = r.ReadU64();
            BanquetModel.Instance.AllExp = allExp;
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_EXP_UPDATE, allExp);
            GameLog.Info("Banquet", "17275 婚礼获得总经验 allExp={0}", allExp);
        }

        // ---- 17276 婚礼开始推送(无 read,S2C only) ----

        /// <summary>对标老端 On17276:无条件重发 17249+17250(role_id_m/role_id_w 本身老端也不消费,
        /// 仅作触发信号)。</summary>
        private void On17276(NetReader r)
        {
            long roleIdM = r.ReadU64();
            long roleIdW = r.ReadU64();
            SendFmt(Proto.BANQUET_WEDDING_STATE); // 17249
            SendFmt(Proto.BANQUET_APPLY_INFO);    // 17250
            GameLog.Info("Banquet", "17276 婚礼开始推送 roleIdM={0} roleIdW={1}(无条件重发17249+17250)", roleIdM, roleIdW);
        }

        // ---- 17277 气氛值变化推送(无 read,S2C only) ----

        private void On17277(NetReader r)
        {
            List<(int type, long values)> list = r.ReadArray(rr => ((int)rr.ReadU8(), (long)rr.ReadU32()));
            foreach ((int type, long values) e in list)
            {
                if (e.type == 1) // 老端仅 type==1 对应气氛值,其余 type 现无分支,仍需读完整个数组保游标
                {
                    BanquetModel.Instance.AuraValue = e.values;
                    EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_AURA_UPDATE, e.values);
                }
            }
            GameLog.Info("Banquet", "17277 气氛值变化推送 count={0}", list.Count);
        }

        // ---- 17278 气氛值奖励推送(无 read,S2C only) ----

        private void On17278(NetReader r)
        {
            long auraNum = r.ReadU32();
            List<BanquetModel.RewardEntry> reward = r.ReadArray(ReadRewardEntry);
            BanquetModel m = BanquetModel.Instance;
            m.LastAuraNum = auraNum;
            m.LastAuraReward.Clear();
            m.LastAuraReward.AddRange(reward);
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_AURA_REWARD_PUSH, auraNum);
            GameLog.Info("Banquet", "17278 气氛值奖励推送 auraNum={0} rewardN={1}", auraNum, reward.Count);
        }

        private static BanquetModel.RewardEntry ReadRewardEntry(NetReader r) => new BanquetModel.RewardEntry
        {
            Type = r.ReadU8(), TypeId = r.ReadU32(), Num = r.ReadU32(),
        };

        // ---- 17279 吃桌菜奖励推送(无 read,S2C only) ----

        private void On17279(NetReader r)
        {
            int type = r.ReadU8();
            List<BanquetModel.RewardEntry> reward = r.ReadArray(ReadRewardEntry);
            BanquetModel m = BanquetModel.Instance;
            m.LastTableRewardType = type;
            m.LastTableReward.Clear();
            m.LastTableReward.AddRange(reward);
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_TABLE_REWARD_PUSH, type);
            GameLog.Info("Banquet", "17279 吃桌菜奖励推送 type={0} rewardN={1}", type, reward.Count);
        }

        // ---- 17298 一键邀请剩余宾客 ----

        /// <summary>一键邀请剩余宾客(C2S 空包,对标 lib_marriage:one_invite_role)。</summary>
        public void RequestOneInvite() => SendFmt(Proto.BANQUET_ONE_INVITE);

        // 17298: error_code:i(**字段名 ErrorCode 非 Code,语义相同**)。
        private void On17298(NetReader r)
        {
            int errorCode = r.ReadI32();
            bool success = errorCode == 1;
            if (success)
            {
                TipsManager.Toast("一键邀请成功！");
                SendFmt(Proto.BANQUET_INVITE_INFO); // 重发 17252
                RequestOpenAskInvite(new[] { 2 });  // 重发 17260,TypeList=[2]
            }
            else
            {
                ShowError(errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_BANQUET_ONE_INVITE_RESULT, success);
            GameLog.Info("Banquet", "17298 一键邀请剩余宾客 errorCode={0}", errorCode);
        }
    }
}
