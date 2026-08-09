using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// 公会核心一期协议控制器(自动循环 轮13a;对标老端 commonController/GuildController.ts 第1组"基础/成员/
    /// 申请/职位/改名/合并"33活号,r13_server_pt400.md §字段序为 wire 权威)。与既有
    /// <see cref="GuildJoinController"/>(40001/03/04/30008,结社列表/建会)并存,注册号互不重叠——40002 单个
    /// 申请也归 GuildJoinController(既有号不迁移原则)。
    ///
    /// 死号严禁实现:40024/25/26(捐献操作,pp_guild handle 已注释)/40041(研究技能,同款断链)。
    /// 40019(公告编辑界面)老端 handler 函数体为空且从无主动请求点,本控制器仅注册防御 no-op,不发送。
    /// </summary>
    public sealed class GuildController : BaseController
    {
        public static readonly GuildController Instance = new GuildController();
        private GuildController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GUILD_ERROR, On40000);
            RegisterProtocal(Proto.GUILD_BASE_INFO, On40005);
            RegisterProtocal(Proto.GUILD_MEMBER_LIST, On40006);
            RegisterProtocal(Proto.GUILD_QUIT, On40007);
            RegisterProtocal(Proto.GUILD_APPLY_LIST, On40008);
            RegisterProtocal(Proto.GUILD_APPLY_APPROVE, On40009);
            RegisterProtocal(Proto.GUILD_APPLY_SETTING_INFO, On40010);
            RegisterProtocal(Proto.GUILD_APPLY_SETTING_SET, On40011);
            RegisterProtocal(Proto.GUILD_ANNOUNCE_SET, On40012);
            RegisterProtocal(Proto.GUILD_APPOINT_POSITION, On40013);
            RegisterProtocal(Proto.GUILD_KICK, On40014);
            RegisterProtocal(Proto.GUILD_SELF_INFO, On40015);
            RegisterProtocal(Proto.GUILD_APPLY_BULK_HANDLE, On40016);
            RegisterProtocal(Proto.GUILD_SCENE_BROADCAST, On40017);
            RegisterProtocal(Proto.GUILD_UPGRADE, On40018);
            RegisterProtocal(Proto.GUILD_ANNOUNCE_INFO, On40019);
            RegisterProtocal(Proto.GUILD_SALARY, On40020);
            RegisterProtocal(Proto.GUILD_PERMISSION_LIST, On40021);
            RegisterProtocal(Proto.GUILD_DONATE_INFO, On40023);
            RegisterProtocal(Proto.GUILD_DISBAND, On40027);
            RegisterProtocal(Proto.GUILD_ACTIVITY, On40028);
            RegisterProtocal(Proto.GUILD_PRESTIGE_INFO, On40030);
            RegisterProtocal(Proto.GUILD_PRESTIGE_DAILY, On40031);
            RegisterProtocal(Proto.GUILD_DONATE_PUSH, On40039);
            RegisterProtocal(Proto.GUILD_SKILL_LIST, On40040);
            RegisterProtocal(Proto.GUILD_SKILL_LEARN, On40042);
            RegisterProtocal(Proto.GUILD_RENAME, On40043);
            RegisterProtocal(Proto.GUILD_RENAME_INFO, On40044);
            RegisterProtocal(Proto.GUILD_BOSS_CALL, On40060);
            RegisterProtocal(Proto.GUILD_MERGE_LIST, On40061);
            RegisterProtocal(Proto.GUILD_MERGE_APPLY, On40062);
            RegisterProtocal(Proto.GUILD_MERGE_RESPONSE, On40063);
            // 40029(调戏)recv:null(服务端无 write 调用点),只发不收,故不注册。

            // ---- 公会二期(轮13b):结社仓库(pt_401) ----
            RegisterProtocal(Proto.GUILD_DEPOT_ERROR, On40100);
            RegisterProtocal(Proto.GUILD_DEPOT_INFO, On40101);
            RegisterProtocal(Proto.GUILD_DEPOT_DONATE, On40102);
            RegisterProtocal(Proto.GUILD_DEPOT_EXCHANGE, On40103);
            RegisterProtocal(Proto.GUILD_DEPOT_DESTROY, On40104);
            RegisterProtocal(Proto.GUILD_DEPOT_GOODS_ADD, On40105);
            RegisterProtocal(Proto.GUILD_DEPOT_GOODS_NUM, On40106);
            RegisterProtocal(Proto.GUILD_DEPOT_RECORD_PUSH, On40107);
            RegisterProtocal(Proto.GUILD_DEPOT_CHANGE, On40108);
            RegisterProtocal(Proto.GUILD_DEPOT_AUTO_DESTROY_INFO, On40110);
            // 40109(按条件销毁)recv:null(服务端无 write(40109,...) 子句,响应借道40104),故不注册。

            // ---- 公会二期(轮13b):结社宝箱(pt_403) ----
            RegisterProtocal(Proto.GUILD_BOX_ERROR, On40300);
            RegisterProtocal(Proto.GUILD_BOX_INFO, On40301);
            RegisterProtocal(Proto.GUILD_BOX_RECEIVE, On40302);
            RegisterProtocal(Proto.GUILD_BOX_NEW_PUSH, On40303);
            RegisterProtocal(Proto.GUILD_BOX_REMOVE_PUSH, On40304);
            RegisterProtocal(Proto.GUILD_BOX_TASK_INFO_PUSH, On40305);

            // ---- 公会二期(轮13b):结社协助(pt_404) ----
            RegisterProtocal(Proto.GUILD_ASSIST_LAUNCH, On40401);
            RegisterProtocal(Proto.GUILD_ASSIST_HELP, On40402);
            RegisterProtocal(Proto.GUILD_ASSIST_CANCEL, On40403);
            RegisterProtocal(Proto.GUILD_ASSIST_COUNT, On40404);
            RegisterProtocal(Proto.GUILD_ASSIST_LIST, On40405);
            RegisterProtocal(Proto.GUILD_ASSIST_NEW_PUSH, On40406);
            RegisterProtocal(Proto.GUILD_ASSIST_REMOVE_PUSH, On40407);
            RegisterProtocal(Proto.GUILD_ASSIST_MY_INFO, On40408);
            RegisterProtocal(Proto.GUILD_ASSIST_SUCCESS_PUSH, On40409);
            RegisterProtocal(Proto.GUILD_ASSIST_ACCEPTED_PUSH, On40410);

            // ---- 公会二期(轮13b):结社武魂/神像(pt_405) ----
            RegisterProtocal(Proto.GUILD_GOD_ERROR, On40500);
            RegisterProtocal(Proto.GUILD_GOD_INFO, On40501);
            RegisterProtocal(Proto.GUILD_GOD_RUNE_INFO, On40502);
            RegisterProtocal(Proto.GUILD_GOD_COLOR_UP, On40503);
            RegisterProtocal(Proto.GUILD_GOD_AWAKE, On40504);
            RegisterProtocal(Proto.GUILD_GOD_RUNE_UPGRADE, On40508);
            RegisterProtocal(Proto.GUILD_GOD_ACHIEVEMENT_ACTIVATE, On40509);
            // 40505(穿戴结果)/40507(脱铭文结果):DEAD,全仓库排除四参遮蔽后确认无调用点,不注册接收器。
            // 40506(激活组合结果):协议层设计上无 write 方向(write 子句列表里没有 40506),不注册接收器。

            // ---- ServerClock(轮20 P4)补 DAY_CHANGE/REFRESH_SERVER_TIME/HOUR_REFRESH 三个复拉钩子
            // (对标老端 GuildController.ts:223/228/232;CHANGE_LEVEL 钩子[ts:215-222]不在本轮范围,不接)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnServerTimeRefresh);
            EventDispatcher.On<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnServerTimeRefresh);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);
            GuildModel.Instance.Reset();
            base.Dispose();
        }

        // ==================== ServerClock(轮20 P4):跨天/整点复拉 ====================

        /// <summary>跨天(对标老端 GuildController.ts:223-227 DAY_CHANGE 绑定,函数体 3 行):
        /// ① Fire(GuildEvent.CHANGED_CONDS_ASSIST)——纯本地事件,通知"协助开放条件"相关 UI(自动战斗/BOSS
        /// 伤害榜/秘境合并挑战等多处入口)按新的开服天/等级复评是否显示协助按钮,本端复用既有
        /// EVT_GUILD_ASSIST_UPDATE(各 Assist 协议回执统一在发的通用事件,语义等价"协助状态变了请刷新");
        /// ② RequestGuildIdol()——见 <see cref="RequestGuildIdol"/>;
        /// ③ CheckApplyRedMask()(GuildModel.ts:2048-2050)是纯本地红点计算(加入公会提示,驱动
        /// RedDotController.up),不发任何协议,本仓 Guild 红点体系未建,数据层轮不镜像(同 On33104
        /// SuperGiftView 先例,CustomActivityController.Core.cs:151)。
        /// ⚠**与下方 OnServerTimeRefresh 在 EVT_GUILD_ASSIST_UPDATE 上双发,核实为老端原生行为、
        /// 非本端误镜像,不去重**:老端 DAY_CHANGE(ts:223-227)与 REFRESH_SERVER_TIME(ts:228-230)
        /// 两个订阅**各自独立**都 Fire(CHANGED_CONDS_ASSIST)(两处源码原文都有这行,不是共享同一次
        /// Fire)。而 0 点时两个事件本就会被同一次 10201 回包连续触发同一帧:老端
        /// ServerTimeModel.InitServerTime(ServerTimeModel.ts:33-41)先调 TryFireEvent()(内部按
        /// lastDay 变化**条件性**触发 DAY_CHANGE,ts:47-51)、再**无条件** Fire(REFRESH_SERVER_TIME)
        /// (ts:40)——本端 GameStartController.On10201(GameStartController.cs:132-135)逐行对应镜像同一
        /// 顺序(ServerTimeModel.TryFireEvent() 后紧跟无条件 Emit(EVT_SERVER_TIME_REFRESH))。故跨天时
        /// CHANGED_CONDS_ASSIST 在老端本就真实触发两次,本端此处双发是对老端行为的忠实复刻,不是重复
        /// 订阅同一语义、也不是接线失误。</summary>
        private void OnServerDayChange()
        {
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
            RequestGuildIdol();
        }

        /// <summary>对标老端 GuildController.ts:228-230 REFRESH_SERVER_TIME 绑定:仅
        /// Fire(GuildEvent.CHANGED_CONDS_ASSIST),同上复用 EVT_GUILD_ASSIST_UPDATE。
        /// ⚠与上方 OnServerDayChange 的双发说明见其类注释——0 点场景下二者同帧各自真实触发一次,
        /// 均属老端原文行为,不去重。</summary>
        private void OnServerTimeRefresh()
        {
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
        }

        /// <summary>对标老端 GuildController.ts:232-238 HOUR_REFRESH 绑定:hour==4 时
        /// setTimeout(reqRewardBox,5)。5ms 延时对标老端 setTimeout(...,5) 原样保留(同
        /// WelfareController.OnServerDayChange 先例,用 <see cref="TimeUtil.Delay"/> 代替——WebGL 上
        /// Task.Delay 永不醒,同项目既有铁律)。</summary>
        private async void OnServerHourRefresh(int hour)
        {
            if (hour != 4) return;
            await TimeUtil.Delay(5);
            await ReqRewardBox();
        }

        /// <summary>对标老端 reqRewardBox(GuildController.ts:324-333):三重前置校验全过才发 40301——
        /// ① ConfigFuncOpenCondition["GuildRewardBoxView"] 存在且 open_lv/open_day 达标
        /// (<see cref="FuncOpenConfig.CheckFuncOpenState"/>,语义等价老端 box_cfg &amp;&amp;
        /// lev&gt;=open_lv &amp;&amp; open_day&gt;=open_day,实测该表 open_lv=130/open_day=1);
        /// ② guild_id&gt;0(已入会,对标 RoleManager.GetMainRoleVo().guild_id&gt;0);
        /// ③ !_rewardBoxViewData(老端"本地还没有宝箱数据才发",本端用 <see cref="GuildModel.HasBoxInfo"/>
        /// 取反镜像,避免重复请求覆盖本地已领取状态)。
        /// ⚠配表未就绪前置判断:老端 `box_cfg = cfg['GuildRewardBoxView']` 缺表/缺条目时 box_cfg 为
        /// undefined,`if (box_cfg && ...)` 直接短路不发 40301。FuncOpenConfig.CheckFuncOpenState 在
        /// `_cfg == null` 时却返回 true(FuncOpenConfig.cs:57,表未加载按开放处理),若不加显式判断会把
        /// "配表未就绪"误判成"条件已开放"、绕过①②直接放行——与老端方向相反。故这里在
        /// CheckFuncOpenState 之前先判 <see cref="FuncOpenConfig.IsLoaded"/>,未就绪就不发。</summary>
        private async System.Threading.Tasks.Task ReqRewardBox()
        {
            await FuncOpenConfig.EnsureLoaded();
            if (!FuncOpenConfig.IsLoaded)
            {
                GameLog.Error("Guild", "reqRewardBox ConfigFuncOpenCondition 未就绪,门槛判定中断,不补发40301(对标老端 box_cfg undefined 短路)");
                return;
            }
            if (!FuncOpenConfig.CheckFuncOpenState("GuildRewardBoxView")) return;
            if (Shenxiao.Module.Core.Role.RoleModel.Instance.GuildId <= 0) return;
            if (GuildModel.Instance.HasBoxInfo) return;
            RequestBoxInfo();
            GameLog.Info("Guild", "reqRewardBox 三重校验通过,补发40301(对标老端 GuildController.ts:324-333)");
        }

        /// <summary>对标老端 RequestGuildIdol(GuildController.ts:317-322):GuildIdolIsOpen() 门槛过了才发
        /// 40501。GuildIdolIsOpen(GuildModel.ts:1103-1115)读服务端 KV 表 config_guild_god_kv 的
        /// open_day/lv_limit 两行,本端复用既有 <see cref="GuildConfigs.GetGodKv"/>(此前零消费方)。</summary>
        public async void RequestGuildIdol()
        {
            await GuildConfigs.EnsureLoaded();
            if (!GuildModel.IsGuildIdolOpen()) return;
            RequestGodList();
        }

        private static void ShowError(int errorCode) => TipsManager.Toast("错误(" + errorCode + ")"); // 错误码表未移植,显码降级

        // ==================== 批量拉取(对标老端 RequestBaseInfo,本轮范围子集) ====================

        /// <summary>进入公会主界面时批量拉取(对标老端 GuildController.RequestBaseInfo):
        /// 40005 基础信息/40021 权限/40023 捐献(数据层)/40040 技能(基础档)/40030 声望/40015 自身信息/
        /// 40061 合并候选/40405 求助列表(13b 新补,数据层已通、消费成本为零);
        /// 40231 守卫/40301 宝箱(视图开页即拉,门控后补)/40501 武魂(同)/40101 仓库(视图开页即拉)仍留后续轮次统一接红点门控。</summary>
        public void RequestBaseInfo()
        {
            SendFmt(Proto.GUILD_BASE_INFO);
            SendFmt(Proto.GUILD_PERMISSION_LIST);
            SendFmt(Proto.GUILD_DONATE_INFO);
            SendFmt(Proto.GUILD_SKILL_LIST, "c", 1);
            SendFmt(Proto.GUILD_PRESTIGE_INFO);
            SendFmt(Proto.GUILD_SELF_INFO);
            SendFmt(Proto.GUILD_MERGE_LIST);
            RequestAssistList();
            GameLog.Info("Guild", "RequestBaseInfo 批量拉取(40005/21/23/40+40030/15/61/405)");
        }

        // ==================== 基础信息/成员 ====================

        public void RequestMembers() => SendFmt(Proto.GUILD_MEMBER_LIST);

        /// <summary>退出结社(对标老端 GuildMemberItem.ClickOut,需二次确认——本控制器只发协议,
        /// 确认弹层由调用方 View 负责)。</summary>
        public void Quit() => SendFmt(Proto.GUILD_QUIT);

        public void RequestApplyList() => SendFmt(Proto.GUILD_APPLY_LIST);

        /// <summary>审批单条申请(发 "lc" role_id, type;type: 1=同意 0=拒绝,对标老端 40009)。</summary>
        public void ApproveApply(long roleId, int type) => SendFmt(Proto.GUILD_APPLY_APPROVE, "lc", roleId, type);

        public void RequestApproveSetting() => SendFmt(Proto.GUILD_APPLY_SETTING_INFO);

        /// <summary>设置审批规则(发 "chi" approve_type, auto_approve_lv, auto_approve_power)。</summary>
        public void SetApproveSetting(int approveType, int autoApproveLv, long autoApprovePower)
            => SendFmt(Proto.GUILD_APPLY_SETTING_SET, "chi", approveType, autoApproveLv, autoApprovePower);

        /// <summary>编辑公告(发 "cs" save_type[1保存/2保存并通知], announce)。</summary>
        public void SetAnnounce(int saveType, string announce) => SendFmt(Proto.GUILD_ANNOUNCE_SET, "cs", saveType, announce);

        /// <summary>任命职位/转让会长(发 "lc" role_id, position)。</summary>
        public void AppointPosition(long roleId, int position) => SendFmt(Proto.GUILD_APPOINT_POSITION, "lc", roleId, position);

        public void Kick(long roleId) => SendFmt(Proto.GUILD_KICK, "l", roleId);

        /// <summary>全部批准(type=1)/全部拒绝(type=2)申请(对标老端 GuildApplyLookView _btn_pass/_btn_refuse)。</summary>
        public void BulkHandleApply(int type) => SendFmt(Proto.GUILD_APPLY_BULK_HANDLE, "c", type);

        public void RequestSalary() => SendFmt(Proto.GUILD_SALARY);

        public void Disband() => SendFmt(Proto.GUILD_DISBAND);

        /// <summary>调戏(发 "l" role_id;recv:null,纯发不接)。</summary>
        public void Tease(long roleId) => SendFmt(Proto.GUILD_TEASE, "l", roleId);

        // ==================== 技能/改名/合并 ====================

        /// <summary>公会技能列表(发 "c" type:1基础/2高级)。</summary>
        public void RequestSkills(int type) => SendFmt(Proto.GUILD_SKILL_LIST, "c", type);

        public void LearnSkill(int skillId) => SendFmt(Proto.GUILD_SKILL_LEARN, "i", skillId);

        public void Rename(string newName) => SendFmt(Proto.GUILD_RENAME, "s", newName);

        public void RequestRenameInfo() => SendFmt(Proto.GUILD_RENAME_INFO);

        public void CallBoss() => SendFmt(Proto.GUILD_BOSS_CALL);

        public void RequestMergeList() => SendFmt(Proto.GUILD_MERGE_LIST);

        public void ApplyMerge(long guildId) => SendFmt(Proto.GUILD_MERGE_APPLY, "l", guildId);

        /// <summary>响应合并申请(发 "cl" op_type[1同意/2拒绝], guild_id)。</summary>
        public void RespondMerge(int opType, long guildId) => SendFmt(Proto.GUILD_MERGE_RESPONSE, "cl", opType, guildId);

        // ==================== recv handlers ====================

        /// <summary>共享错误壳(对标老端 on40000:仅显码,无业务)。40013任命互斥/40029自嘲/40042未入会/
        /// 40043改名checklist 等前置粗校验失败均走这里,无法辨识来源,统一显码降级。</summary>
        private void On40000(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            ShowError(errorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ERROR, errorCode);
            GameLog.Info("Guild", "40000 共享错误壳 errorCode={0}", errorCode);
        }

        /// <summary>40005:guild_id:l, guild_name:s, announce:s, position_list[u16×{position:c,role_id:l,figure}],
        /// guild_lv:h, gfunds:i, growth_val:i, gactivity:i, member_num:h, member_capacity:h, combat_power:l,
        /// online_num:h, disband_warnning_time:i, salary_status:c, division:c, join_time:i, is_in_merge:c。
        /// 首次到达(本地无缓存)时把公告转发进公会聊天频道(对标老端 on40005:`if (!ginfo) ChatModel.GuildChat(announce)`)。</summary>
        private void On40005(NetReader r)
        {
            var info = new GuildModel.GuildInfo
            {
                GuildId = r.ReadU64(),
                GuildName = r.ReadString(),
                Announce = r.ReadString(),
            };
            int posCount = r.ReadU16();
            for (int i = 0; i < posCount; i++)
            {
                var entry = new GuildModel.PositionEntry { Position = r.ReadU8(), RoleId = r.ReadU64(), Figure = FigureProto.Read(r) };
                info.PositionList.Add(entry);
            }
            info.GuildLv = r.ReadU16();
            info.Gfunds = r.ReadU32();
            info.GrowthVal = r.ReadU32();
            info.Gactivity = r.ReadU32();
            info.MemberNum = r.ReadU16();
            info.MemberCapacity = r.ReadU16();
            info.CombatPower = r.ReadU64();
            info.OnlineNum = r.ReadU16();
            info.DisbandWarnningTime = r.ReadU32();
            info.SalaryStatus = r.ReadU8();
            info.Division = r.ReadU8();
            info.JoinTime = r.ReadU32();
            info.IsInMerge = r.ReadU8();

            bool isFirstInfo = !GuildModel.Instance.HasInfo; // 对标老端 on40005:`if (!ginfo)` 首次到达才转发公告进聊天
            GuildModel.Instance.SetInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_INFO_UPDATE);
            if (isFirstInfo && !string.IsNullOrEmpty(info.Announce))
            {
                Shenxiao.Module.Core.Chat.ChatModel.Instance.AddMessage(new Shenxiao.Module.Core.Chat.ChatMessage
                {
                    Channel = Shenxiao.Module.Core.Chat.ChatModel.ChannelGuild,
                    Message = info.Announce,
                    Result = 1,
                });
            }
            GameLog.Info("Guild", "40005 基础信息 guildId={0} name={1} lv={2} member={3}/{4} remaining={5}B",
                info.GuildId, info.GuildName, info.GuildLv, info.MemberNum, info.MemberCapacity, r.Remaining);
        }

        /// <summary>40006:member_list[u16×{role_id:l,figure,position:c,title_id:i,combat_power:l,
        /// online_flag:c,offline_time:i,create_time:i}]。服务端无分页,规模上限=member_capacity。</summary>
        private void On40006(NetReader r)
        {
            List<GuildModel.MemberEntry> list = r.ReadArray(ReadMemberEntry);
            long selfRoleId = Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId;
            GuildModel.Instance.SetMembers(list, selfRoleId);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_MEMBER_UPDATE);
            GameLog.Info("Guild", "40006 成员列表 count={0} remaining={1}B", list.Count, r.Remaining);
        }

        private static GuildModel.MemberEntry ReadMemberEntry(NetReader r)
        {
            return new GuildModel.MemberEntry
            {
                RoleId = r.ReadU64(),
                Figure = FigureProto.Read(r),
                Position = r.ReadU8(),
                TitleId = (int)r.ReadU32(),
                CombatPower = r.ReadU64(),
                Online = r.ReadU8() != 0,
                OfflineTime = r.ReadU32(),
                CreateTime = r.ReadU32(),
            };
        }

        /// <summary>40007 退出结社:error_code:i。成功→清 RoleModel 公会身份 + GuildModel.Reset + 关闭
        /// 公会主界面(对标老端 on40007 CLOSE_VIEW 'GuildMainBaseView',否则主窗残留空数据僵尸画面)。</summary>
        private void On40007(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode == 1)
            {
                Shenxiao.Module.Core.Role.RoleModel.Instance.SetGuildIdentity(0, "", 0, "");
                GuildModel.Instance.Reset();
                GuildMainFlow.Close();
                TipsManager.Toast("成功退出结社");
                Shenxiao.Module.Core.Daily.DailyController.Instance.RequestSignUpList();
                GameLog.Info("Guild", "40007 退出成功");
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40007 退出失败 errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_UPDATE);
        }

        /// <summary>40008:apply_list[u16×{role_id:l,figure,combat_power:l}]。对标老端 apply_request_mark:
        /// 若由"查看申请"点击触发(标记置位),到达时非空自动开申请弹层、为空 toast。</summary>
        private void On40008(NetReader r)
        {
            List<GuildModel.ApplyEntry> list = r.ReadArray(ReadApplyEntry);
            GuildModel.Instance.SetApplies(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_APPLY_UPDATE);
            if (GuildModel.Instance.ApplyRequestMark)
            {
                GuildModel.Instance.ApplyRequestMark = false;
                if (list.Count > 0) EventDispatcher.Emit(GlobalEvent.EVT_GUILD_APPLY_AUTO_OPEN);
                else TipsManager.Toast("当前没有申请信息");
            }
            GameLog.Info("Guild", "40008 申请列表 count={0} remaining={1}B", list.Count, r.Remaining);
        }

        private static GuildModel.ApplyEntry ReadApplyEntry(NetReader r)
        {
            return new GuildModel.ApplyEntry { RoleId = r.ReadU64(), Figure = FigureProto.Read(r), CombatPower = r.ReadU64() };
        }

        /// <summary>40009:error_code:i, type:c, role_id:l。成功→**订正删单条**(rule10,见 GuildModel.RemoveApply)。
        /// **勘误**:深层校验失败(审批人不存在/无权限/申请记录不存在等)并非静默——lib_guild_mod.erl 结尾
        /// write(40009,...) 对 check_approve_guild_apply 的成功/失败两条分支都无条件执行,失败码会正常回包
        /// 到这里(唯一真静默是 pp_guild.erl get_role_show==[] 更前置的场景);下方 errorCode!=1 分支本就
        /// 正确处理,此注释仅为避免后续误当"静默"设计死等逻辑。</summary>
        private void On40009(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int type = r.ReadU8();
            long roleId = r.ReadU64();
            if (errorCode == 1)
            {
                GuildModel.Instance.RemoveApply(roleId);
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_APPLY_UPDATE);
                GameLog.Info("Guild", "40009 审批成功 roleId={0} type={1}", roleId, type);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40009 审批失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40010:approve_type:c, auto_approve_lv:h, auto_approve_power:i(纯数据推送,无 error_code)。</summary>
        private void On40010(NetReader r)
        {
            int approveType = r.ReadU8();
            int autoLv = r.ReadU16();
            long autoPower = r.ReadU32();
            GuildModel.Instance.SetApproveSetting(approveType, autoLv, autoPower);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
            GameLog.Info("Guild", "40010 审批设置 type={0} lv={1} power={2}", approveType, autoLv, autoPower);
        }

        /// <summary>40011:error_code:i。**订正**:pp_guild 前置层 ErrorCode==nothing 时确实 skip 自己不发,
        /// 但已 cast 出去的业务层(mod_guild_cast.erl 'setting_approve')在函数末尾无条件 write(40011,...),
        /// 成功时 ErrorCode=?SUCCESS=1 一样会回包——绝非"收到即失败"。errorCode==1→成功(对标老端
        /// GuildController.ts on40011 toast'设置成功');否则显码降级。</summary>
        private void On40011(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode == 1)
            {
                TipsManager.Toast("设置成功");
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
                GameLog.Info("Guild", "40011 设置审批规则成功");
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40011 设置审批规则失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40012:error_code:i。**订正(同40011)**:mod_guild_cast.erl 'modify_announce' 结尾无条件
        /// write(40012,...),成功时 ErrorCode=1 会正常回包,并非静默。errorCode==1→成功,补发40005刷新
        /// 公告显示(对标老端 GuildController.ts on40012 SendFmtToGame(40005)+toast'修改成功');
        /// 唯一真等级门:公会等级&lt;4 拒(err400_guild_level_not_enough)。</summary>
        private void On40012(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode == 1)
            {
                SendFmt(Proto.GUILD_BASE_INFO);
                TipsManager.Toast("修改成功");
                GameLog.Info("Guild", "40012 编辑公告成功");
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40012 编辑公告失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40013:error_code:i, role_id:l, position:c。成功→补发 40006 刷新成员列表(对标老端)。</summary>
        private void On40013(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long roleId = r.ReadU64();
            int position = r.ReadU8();
            if (errorCode == 1)
            {
                RequestMembers();
                GameLog.Info("Guild", "40013 任命成功 roleId={0} position={1}", roleId, position);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40013 任命失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40014:error_code:i, role_id:l。成功→补发 40006(对标老端)。</summary>
        private void On40014(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long roleId = r.ReadU64();
            if (errorCode == 1)
            {
                RequestMembers();
                GameLog.Info("Guild", "40014 踢出成功 roleId={0}", roleId);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40014 踢出失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40015:guild_id:l, guild_name:s, guild_lv:h, position:c, position_name:s。落 RoleModel 主角VO
        /// (对标老端 mainRoleVo ChangeVar 四件套);position==3(会员)灭申请红点(本轮无红点系统,跳过);
        /// position∈{1,2,4}(会长/副会长/宝贝)补发 40008 查申请列表。</summary>
        private void On40015(NetReader r)
        {
            long guildId = r.ReadU64();
            string guildName = r.ReadString();
            int guildLv = r.ReadU16();
            int position = r.ReadU8();
            string positionName = r.ReadString();

            Shenxiao.Module.Core.Role.RoleModel.Instance.SetGuildIdentity(guildId, guildName, position, positionName);
            if (position == 1 || position == 2 || position == 4) RequestApplyList();
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_UPDATE);
            GameLog.Info("Guild", "40015 自身信息 guildId={0} name={1} lv={2} position={3}({4})",
                guildId, guildName, guildLv, position, positionName);
        }

        /// <summary>40016:error_code:i, type:c。成功→补发40006 + 本地清空申请列表(对标老端)。
        /// **Type 严禁发 {1,2} 以外的值**(服务端子句不匹配=静默丢弃)。</summary>
        private void On40016(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int type = r.ReadU8();
            if (errorCode == 1)
            {
                RequestMembers();
                GuildModel.Instance.ClearApplies();
                TipsManager.Toast("操作成功");
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_APPLY_UPDATE);
                GameLog.Info("Guild", "40016 批量处理成功 type={0}", type);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40016 批量处理失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40017 场景广播(纯推送):role_id:l, guild_id:l, guild_name:s, position:c, position_name:s。
        /// 按地图区域池广播(非公会广播),用于更新**他人**场景头顶名牌——Common/UI3D 红线内不接场景消费,
        /// 仅正确解析 + 事件分发(别把这条扇出包误当全量公会数据刷新)。</summary>
        private void On40017(NetReader r)
        {
            var tag = new GuildModel.SceneGuildTag
            {
                RoleId = r.ReadU64(),
                GuildId = r.ReadU64(),
                GuildName = r.ReadString(),
                Position = r.ReadU8(),
                PositionName = r.ReadString(),
            };
            GameLog.Info("Guild", "40017 场景广播(TODO 场景消费方,红线内不接 UI3D) roleId={0} guildName={1}",
                tag.RoleId, tag.GuildName);
        }

        /// <summary>40018:error_code:i。**必接 recv**——操作者私有确认(带真实失败码)+ 等级真变化时
        /// 公会全员广播(固定成功[1]);两者字段shape相同,按"到达即刷新"处理,不辨来源(见 Proto 注释)。
        /// 老端"升级仙宗"按钮从未真实发送 40018,本轮同样不做发送 API。</summary>
        private void On40018(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode == 1)
            {
                SendFmt(Proto.GUILD_BASE_INFO); // 对标老端 on40018:成功即补发 40005 刷新等级显示
                GameLog.Info("Guild", "40018 公会升级(广播或私有确认) 成功");
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40018 公会升级失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40019(纯死号,老端 handler 函数体为空且从无主动请求点):remain_times:c, free_times:c。
        /// 仅注册防御 no-op,本控制器从不发送该号,理论上不会被调度。</summary>
        private void On40019(NetReader r)
        {
            r.ReadU8();
            r.ReadU8();
        }

        /// <summary>40020 领工资:error_code:i。成功→标记 salary_status=1 + 补发40005 刷新(对标老端;
        /// 声望头衔奖励弹窗未接,本轮跳过 CongratulationObtainView)。</summary>
        private void On40020(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode == 1)
            {
                if (GuildModel.Instance.Info != null) GuildModel.Instance.Info.SalaryStatus = 1;
                TipsManager.Toast("领取成功");
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_INFO_UPDATE);
                GameLog.Info("Guild", "40020 领工资成功");
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40020 领工资失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40021:permission_type_list[u16×{c}]。不在公会时回空列表(非静默/非报错)。</summary>
        private void On40021(NetReader r)
        {
            List<int> list = r.ReadArray(rr => (int)rr.ReadU8());
            GuildModel.Instance.SetPermissions(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
            GameLog.Info("Guild", "40021 权限列表 count={0}", list.Count);
        }

        /// <summary>40023(数据层保留,UI 不建):gactivity:i, donate_times:c,
        /// self_gift_list[u16×{gift_id:h,gift_status:c}], donate_record[u16×{item_to_bin_6}]
        /// (item_to_bin_6 字段序假设同 40026,报告未逐字段列出,见 Proto 注释标注)。</summary>
        private void On40023(NetReader r)
        {
            long gactivity = r.ReadU32();
            int donateTimes = r.ReadU8();
            List<GuildModel.SelfGift> gifts = r.ReadArray(rr => new GuildModel.SelfGift { GiftId = rr.ReadU16(), GiftStatus = rr.ReadU8() });
            List<GuildModel.DonateRecord> records = r.ReadArray(rr => new GuildModel.DonateRecord
            {
                DonateId = (int)rr.ReadU32(),
                RoleId = rr.ReadU64(),
                RoleName = rr.ReadString(),
                DonateType = rr.ReadU8(),
                Times = rr.ReadU8(),
                DonateAdd = rr.ReadU16(),
                GfundsAdd = rr.ReadU16(),
                GuildActivity = rr.ReadU16(),
                Time = rr.ReadU32(),
            });
            GuildModel.Instance.SetActivity(gactivity);
            GuildModel.Instance.SetDonateInfo(donateTimes, gifts, records);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
            GameLog.Info("Guild", "40023 捐献信息(数据层) gactivity={0} donateTimes={1} gifts={2} records={3}",
                gactivity, donateTimes, gifts.Count, records.Count);
        }

        /// <summary>40027 解散:error_code:i。成功→清 RoleModel 公会身份 + Reset + 关闭公会主界面
        /// (对标老端 on40027 CLOSE_VIEW 'GuildMainBaseView',同 40007)。</summary>
        private void On40027(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode == 1)
            {
                Shenxiao.Module.Core.Role.RoleModel.Instance.SetGuildIdentity(0, "", 0, "");
                GuildModel.Instance.Reset();
                GuildMainFlow.Close();
                TipsManager.Toast("解散结社成功");
                Shenxiao.Module.Core.Daily.DailyController.Instance.RequestSignUpList();
                GameLog.Info("Guild", "40027 解散成功");
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40027 解散失败 errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_UPDATE);
        }

        /// <summary>40028:gactivity:i(纯活跃度查询/推送)。</summary>
        private void On40028(NetReader r)
        {
            long gactivity = r.ReadU32();
            GuildModel.Instance.SetActivity(gactivity);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
        }

        /// <summary>40030:all_prestige:i, title_id:i, prestige_week:i, prestige_limit:i。</summary>
        private void On40030(NetReader r)
        {
            int all = (int)r.ReadU32();
            int titleId = (int)r.ReadU32();
            int week = (int)r.ReadU32();
            int limit = (int)r.ReadU32();
            GuildModel.Instance.SetPrestige(all, titleId, week, limit);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
            GameLog.Info("Guild", "40030 声望信息 all={0} titleId={1}", all, titleId);
        }

        /// <summary>40031:all_prestige:i, prestige_day:i, prestige_day_limit:i。</summary>
        private void On40031(NetReader r)
        {
            int all = (int)r.ReadU32();
            int day = (int)r.ReadU32();
            int dayLimit = (int)r.ReadU32();
            GuildModel.Instance.SetPrestigeDaily(all, day, dayLimit);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
        }

        /// <summary>40039(纯推送,仅被动获得贡献时触发):new_donate:i。</summary>
        private void On40039(NetReader r)
        {
            int donate = (int)r.ReadU32();
            GuildModel.Instance.SetDonate(donate);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
        }

        /// <summary>40040:donate:i, skill_list[u16×{skill_id:i,learn_lv:c,research_lv:c,cur_power:l,next_power:l}]。</summary>
        private void On40040(NetReader r)
        {
            int donate = (int)r.ReadU32();
            List<GuildModel.SkillEntry> list = r.ReadArray(rr => new GuildModel.SkillEntry
            {
                SkillId = (int)rr.ReadU32(),
                LearnLv = rr.ReadU8(),
                ResearchLv = rr.ReadU8(),
                CurPower = rr.ReadU64(),
                NextPower = rr.ReadU64(),
            });
            GuildModel.Instance.SetDonate(donate);
            GuildModel.Instance.SetSkills(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
            GameLog.Info("Guild", "40040 技能列表 donate={0} count={1}", donate, list.Count);
        }

        /// <summary>40042:error_code:i, skill_id:i, learn_lv:c, donate:i(**学习后剩余贡献值,非本次消耗**),
        /// cur_power:l, next_power:l。未入会前置失败走共享40000,这里到达的都是深层业务成功/失败。</summary>
        private void On40042(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int skillId = (int)r.ReadU32();
            int learnLv = r.ReadU8();
            int donate = (int)r.ReadU32();
            long cur = r.ReadU64();
            long next = r.ReadU64();
            if (errorCode == 1)
            {
                GuildModel.Instance.SetDonate(donate);
                GuildModel.Instance.PatchSkill(skillId, learnLv, cur, next);
                TipsManager.Toast("升级技能成功");
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
                GameLog.Info("Guild", "40042 学习成功 skillId={0} learnLv={1}", skillId, learnLv);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40042 学习失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40043:error_code:i, new_name:s。**深层9项checklist失败一律走共享40000,只有真正
        /// 扣费成功才回自己的号**——故这里到达的恒为成功;成功→补发40015 + 事件通知改名(对标老端)。</summary>
        private void On40043(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            string newName = r.ReadString();
            if (errorCode == 1)
            {
                SendFmt(Proto.GUILD_SELF_INFO);
                if (GuildModel.Instance.Info != null) GuildModel.Instance.Info.GuildName = newName;
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_INFO_UPDATE);
                GameLog.Info("Guild", "40043 改名成功 newName={0}", newName);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40043 改名失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40044:is_free:c, next_rename_time:i。</summary>
        private void On40044(NetReader r)
        {
            bool isFree = r.ReadU8() != 0;
            long nextTime = r.ReadU32();
            GuildModel.Instance.SetRenameInfo(isFree, nextTime);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
        }

        /// <summary>40060 仙宗召援(真公会广播):role_id:l, role_name:s, role_lv:h, role_career:c, role_sex:c,
        /// role_pic:s, role_pic_ver:i, boss_type:h, boss_type_name:s, boss_id:i, layer:c, scene_id:i, x:h, y:h。
        /// 非本轮 UI 范围(数据层保留),非自己发起才提示——本轮无 HelpTipsBossView,仅记录 BossCallSelfMark。</summary>
        private void On40060(NetReader r)
        {
            var info = new GuildModel.BossCallInfo
            {
                RoleId = r.ReadU64(),
                RoleName = r.ReadString(),
                RoleLv = r.ReadU16(),
                RoleCareer = r.ReadU8(),
                RoleSex = r.ReadU8(),
                RolePic = r.ReadString(),
                RolePicVer = r.ReadU32(),
                BossType = r.ReadU16(),
                BossTypeName = r.ReadString(),
                BossId = (int)r.ReadU32(),
                Layer = r.ReadU8(),
                SceneId = (int)r.ReadU32(),
                X = r.ReadU16(),
                Y = r.ReadU16(),
            };
            GuildModel.Instance.SetLastBossCall(info);
            bool isSelf = info.RoleId == Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId;
            if (!GuildModel.Instance.BossCallSelfMark && !isSelf)
            {
                GameLog.Info("Guild", "40060 仙宗召援(TODO HelpTipsBossView) from={0} boss={1}", info.RoleName, info.BossTypeName);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
        }

        /// <summary>40061:guild_list[u16×{同40001 item_to_bin_0}](item_to_bin_12,合并候选)。</summary>
        private void On40061(NetReader r)
        {
            List<GuildModel.MergeCandidate> list = r.ReadArray(ReadMergeCandidate);
            GuildModel.Instance.SetMergeCandidates(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
            GameLog.Info("Guild", "40061 合并候选 count={0}", list.Count);
        }

        private static GuildModel.MergeCandidate ReadMergeCandidate(NetReader r)
        {
            return new GuildModel.MergeCandidate
            {
                GuildId = r.ReadU64(),
                GuildName = r.ReadString(),
                GuildLv = r.ReadU16(),
                Gfunds = r.ReadU32(),
                ChiefId = r.ReadU64(),
                ChiefName = r.ReadString(),
                MemberNum = r.ReadU16(),
                MemberCapacity = r.ReadU16(),
                IsApply = r.ReadU8() != 0,
                AutoApprovePower = r.ReadU32(),
                CombatPower = r.ReadU64(),
                MergeStatus = r.ReadU8(),
                MergeRel = r.ReadU8(),
            };
        }

        /// <summary>40062:error_code:i, guild_id:l。</summary>
        private void On40062(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long guildId = r.ReadU64();
            if (errorCode == 1)
            {
                TipsManager.Toast("已申请合并");
                GameLog.Info("Guild", "40062 申请合并成功 guildId={0}", guildId);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40062 申请合并失败 errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
        }

        /// <summary>40063:error_code:i, guild_id:l。成功→补发40005+40061(对标老端)。</summary>
        private void On40063(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long guildId = r.ReadU64();
            if (errorCode == 1)
            {
                SendFmt(Proto.GUILD_BASE_INFO);
                RequestMergeList();
                GameLog.Info("Guild", "40063 响应合并成功 guildId={0}", guildId);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40063 响应合并失败 errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DATA_UPDATE);
        }

        // ============================================================================================
        // 公会二期(自动循环 轮13b):结社仓库(pt_401)/宝箱(pt_403)/协助(pt_404)/武魂神像(pt_405)
        // wire 权威 = yu_server src/pt/pt_40{1,3,4,5}.erl 源码逐字节读出(非报告转述)。
        // ============================================================================================

        // ==================== 结社仓库(pt_401) ====================

        public void RequestDepotInfo() => SendFmt(Proto.GUILD_DEPOT_INFO);

        /// <summary>捐献装备入仓库(自定义变长数组:发 "h"+count,逐条 "li" goods_id,num,对标老端
        /// GuildDepotSelectView 自拼包体)。**Guard**:空列表本地拦截,不发包(对标老端"前端先判空拦截")。</summary>
        public void DonateDepot(IReadOnlyList<(long goodsId, int num)> list)
        {
            if (list == null || list.Count == 0) { TipsManager.Toast("请先选择要捐献的物品"); return; }
            var fmt = new StringBuilder("h");
            var args = new List<object>(1 + list.Count * 2) { list.Count };
            foreach ((long goodsId, int num) it in list)
            {
                fmt.Append("li");
                args.Add(it.goodsId);
                args.Add(it.num);
            }
            SendFmt(Proto.GUILD_DEPOT_DONATE, fmt.ToString(), args.ToArray());
            GameLog.Info("Guild", "40102 捐献仓库 items={0}", list.Count);
        }

        /// <summary>积分兑换仓库物品(发 "lii" goods_id,type_id,num)。**Guard(r13b §Guard 静默陷阱订正)**:
        /// 任务装备(goods_id==DEPOT_TASK_EQUIP_GOODS_ID)锁死 num=1(≠1 会被服务端错误路由到通用兑换分支,
        /// 大概率报"物品不在仓库");其余物品 num 必须&gt;0,否则服务端两个 do_handle 子句都不匹配、真无回包
        /// (本地提前拦截,不寄望回包纠错)。</summary>
        public void ExchangeDepot(long goodsId, int typeId, int num)
        {
            bool isTaskEquip = goodsId == GuildModel.DEPOT_TASK_EQUIP_GOODS_ID;
            if (isTaskEquip) num = 1;
            else if (num <= 0)
            {
                GameLog.Warn("Guild", "40103 本地拦截:num={0}<=0 且非任务装备(服务端会静默无回包)", num);
                return;
            }
            SendFmt(Proto.GUILD_DEPOT_EXCHANGE, "lii", goodsId, typeId, num);
            GameLog.Info("Guild", "40103 兑换 goodsId={0} typeId={1} num={2}", goodsId, typeId, num);
        }

        /// <summary>销毁仓库物品(自定义变长数组:发 "h"+count,逐条 "l" goods_id)。**Guard**:空列表本地拦截。</summary>
        public void DestroyDepot(IReadOnlyList<long> goodsIds)
        {
            if (goodsIds == null || goodsIds.Count == 0) { TipsManager.Toast("请先选择要销毁的物品"); return; }
            var fmt = new StringBuilder("h");
            var args = new List<object>(1 + goodsIds.Count) { goodsIds.Count };
            foreach (long id in goodsIds) { fmt.Append('l'); args.Add(id); }
            SendFmt(Proto.GUILD_DEPOT_DESTROY, fmt.ToString(), args.ToArray());
            GameLog.Info("Guild", "40104 销毁 items={0}", goodsIds.Count);
        }

        /// <summary>设置按条件自动销毁(发 "ccc" stage,color,star;recv:null,响应借道40104——本调用后若需要
        /// 刷新回显,调用方自行补发 RequestAutoDestroySetting)。</summary>
        public void SetAutoDestroySetting(int stage, int color, int star)
        {
            SendFmt(Proto.GUILD_DEPOT_AUTO_DESTROY_SET, "ccc", stage, color, star);
            GameLog.Info("Guild", "40109 设置自动销毁条件 stage={0} color={1} star={2}", stage, color, star);
        }

        public void RequestAutoDestroySetting() => SendFmt(Proto.GUILD_DEPOT_AUTO_DESTROY_INFO);

        private void On40100(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            ShowError(errorCode);
            GameLog.Info("Guild", "40100 仓库错误壳 errorCode={0}", errorCode);
        }

        /// <summary>40101:depot_score:i, exchange_records[u16×16字段], depot_goods[u16×13字段]
        /// (字段序=pt_401.erl item_to_bin_0/_5 源码原文)。</summary>
        private void On40101(NetReader r)
        {
            int depotScore = (int)r.ReadU32();
            List<GuildModel.DepotRecordEntry> records = r.ReadArray(ReadDepotRecord);
            List<GuildModel.DepotGoodsEntry> goods = r.ReadArray(ReadDepotGoods);
            GuildModel.Instance.SetDepotInfo(depotScore, records, goods);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DEPOT_UPDATE);
            GameLog.Info("Guild", "40101 仓库信息 score={0} records={1} goods={2} remaining={3}B",
                depotScore, records.Count, goods.Count, r.Remaining);
        }

        /// <summary>兑换记录单条(item_to_bin_0/_16,16字段;嵌套 addition_attrlist/equip_extra_attr/stone_list/
        /// wash_attr 四个变长数组按序读过不留——本轮仅需 12 个标量字段铺列表文本)。</summary>
        private static GuildModel.DepotRecordEntry ReadDepotRecord(NetReader r)
        {
            var e = new GuildModel.DepotRecordEntry
            {
                RecordId = (int)r.ReadU32(),
                RoleName = r.ReadString(),
                ExchangeType = r.ReadU8(),
                GoodsId = r.ReadU64(),
                TypeId = (int)r.ReadU32(),
                Color = r.ReadU8(),
                Rating = r.ReadU32(),
                OverallRating = r.ReadU32(),
            };
            SkipDepotNestedAttrs(r);
            e.SuitLv = r.ReadU8();
            e.SuitSlv = r.ReadU16();
            e.SuitCount = r.ReadU8();
            e.Time = r.ReadU32();
            return e;
        }

        /// <summary>仓库物品单条(item_to_bin_5/_10,13字段;比记录少 Id/RoleName/ExchangeType/Time,多 GoodsNum)。</summary>
        private static GuildModel.DepotGoodsEntry ReadDepotGoods(NetReader r)
        {
            var e = new GuildModel.DepotGoodsEntry
            {
                GoodsId = r.ReadU64(),
                TypeId = (int)r.ReadU32(),
                Num = r.ReadU32(),
                Color = r.ReadU8(),
                Rating = r.ReadU32(),
                OverallRating = r.ReadU32(),
            };
            SkipDepotNestedAttrs(r);
            e.SuitLv = r.ReadU8();
            e.SuitSlv = r.ReadU16();
            e.SuitCount = r.ReadU8();
            return e;
        }

        /// <summary>装备实例嵌套四件套按序读过不留(addition_attrlist[4字段]/equip_extra_attr[6字段]/
        /// stone_list[2字段]/wash_attr[4字段],字段序=pt_401.erl item_to_bin_1/2/3/4 源码原文;
        /// 实例属性展示待装备 tips 移植,本轮仓库列表只需基础 12/13 字段)。</summary>
        private static void SkipDepotNestedAttrs(NetReader r)
        {
            r.ReadArray(rr => { rr.ReadU8(); rr.ReadU32(); rr.ReadU8(); rr.ReadU32(); return 0; });                  // addition_attrlist
            r.ReadArray(rr => { rr.ReadU8(); rr.ReadU8(); rr.ReadU16(); rr.ReadU32(); rr.ReadU8(); rr.ReadU32(); return 0; }); // equip_extra_attr
            r.ReadArray(rr => { rr.ReadU8(); rr.ReadU32(); return 0; });                                             // stone_list
            r.ReadArray(rr => { rr.ReadU8(); rr.ReadU8(); rr.ReadU16(); rr.ReadU32(); return 0; });                  // wash_attr
        }

        /// <summary>40102:error_code:i, depot_score:i。**该号 ErrorCode 恒为 ?SUCCESS**(服务端已知调用点
        /// 全部如此),失败改走共享40100——else 分支纯防御,理论不可达。</summary>
        private void On40102(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int depotScore = (int)r.ReadU32();
            if (errorCode == 1)
            {
                GuildModel.Instance.SetDepotScore(depotScore);
                TipsManager.Toast("捐献成功");
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DEPOT_UPDATE);
                GameLog.Info("Guild", "40102 捐献成功 depotScore={0}", depotScore);
            }
            else
            {
                GameLog.Warn("Guild", "40102 非预期失败码 errorCode={0}(该号理论恒成功,失败应走40100)", errorCode);
            }
        }

        /// <summary>40103:error_code:i, depot_score:i。失败补发40101刷新(对标老端"防止界面数据脏")。</summary>
        private void On40103(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int depotScore = (int)r.ReadU32();
            if (errorCode == 1)
            {
                GuildModel.Instance.SetDepotScore(depotScore);
                TipsManager.Toast("兑换成功");
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DEPOT_UPDATE);
                GameLog.Info("Guild", "40103 兑换成功 depotScore={0}", depotScore);
            }
            else
            {
                ShowError(errorCode);
                RequestDepotInfo();
                GameLog.Info("Guild", "40103 兑换失败 errorCode={0}", errorCode);
            }
        }

        /// <summary>40104:error_code:i, op_type:c[3手动/4自动], depot_num:i。失败补发40101(对标老端)。</summary>
        private void On40104(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int opType = r.ReadU8();
            int depotNum = (int)r.ReadU32();
            if (errorCode == 1)
            {
                // 对标老端:op_type==3 手动销毁/op_type==4 结社仓管自动清理,两种文案分开(其余 op_type 老端不弹)。
                if (opType == 3) TipsManager.Toast("操作成功，共清理" + depotNum + "件装备");
                else if (opType == 4) TipsManager.Toast("结社仓管自动清理" + depotNum + "件装备");
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DEPOT_UPDATE);
                GameLog.Info("Guild", "40104 销毁成功 opType={0} depotNum={1}", opType, depotNum);
            }
            else
            {
                ShowError(errorCode);
                RequestDepotInfo();
                GameLog.Info("Guild", "40104 销毁失败 errorCode={0}", errorCode);
            }
        }

        private void On40105(NetReader r)
        {
            List<GuildModel.DepotGoodsEntry> list = r.ReadArray(ReadDepotGoods);
            GuildModel.Instance.AddDepotGoods(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DEPOT_UPDATE);
            GameLog.Info("Guild", "40105 仓库新增推送 count={0}", list.Count);
        }

        /// <summary>40106:depot_goods[u16×{goods_id:l,num:i}](精简结构,num=0=删除)。</summary>
        private void On40106(NetReader r)
        {
            List<(long goodsId, long num)> deltas = r.ReadArray(rr => (rr.ReadU64(), (long)rr.ReadU32()));
            GuildModel.Instance.ApplyDepotGoodsNum(deltas);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DEPOT_UPDATE);
            GameLog.Info("Guild", "40106 仓库数量增量 count={0}", deltas.Count);
        }

        private void On40107(NetReader r)
        {
            List<GuildModel.DepotRecordEntry> list = r.ReadArray(ReadDepotRecord);
            GuildModel.Instance.PrependDepotRecords(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DEPOT_UPDATE);
            GameLog.Info("Guild", "40107 兑换记录推送 count={0}", list.Count);
        }

        /// <summary>40108:change:c(四处调用点硬编码恒为1)。change==1 补发40101整表刷新(对标老端)。</summary>
        private void On40108(NetReader r)
        {
            int change = r.ReadU8();
            if (change == 1) RequestDepotInfo();
            GameLog.Info("Guild", "40108 仓库变化广播(公会全员) change={0}", change);
        }

        private void On40110(NetReader r)
        {
            int stage = r.ReadU8();
            int color = r.ReadU8();
            int star = r.ReadU8();
            GuildModel.Instance.SetAutoDestroySetting(stage, color, star);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_DEPOT_UPDATE);
            GameLog.Info("Guild", "40110 自动销毁条件 stage={0} color={1} star={2}", stage, color, star);
        }

        // ==================== 结社宝箱(pt_403) ====================

        public void RequestBoxInfo() => SendFmt(Proto.GUILD_BOX_INFO);

        /// <summary>领取宝箱奖励(发 "l" auto_id——**64位!服务端 mod_id_create 生成的自增id,r13b 裁决:
        /// 老端实发 'l'(64位)与服务端一致——早期侦察稿曾误记老端为 'h'(16位)/称其为"老端bug",
        /// 经复核老端 GuildRBItem.ts/GuildRewardBoxView.ts/proto403.d.ts 三处均为 'l',该误记已订正**;
        /// auto_id=0=一键领取)。</summary>
        public void ReceiveBox(long autoId) => SendFmt(Proto.GUILD_BOX_RECEIVE, "l", autoId);

        private void On40300(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            ShowError(errorCode);
            GameLog.Info("Guild", "40300 宝箱错误壳 errorCode={0}", errorCode);
        }

        private void On40301(NetReader r)
        {
            int num = r.ReadU16();
            int maxNum = r.ReadU16();
            List<GuildModel.BoxSendEntry> sendList = r.ReadArray(ReadBoxSendEntry);
            List<GuildModel.BoxLogEntry> log = r.ReadArray(ReadBoxLogEntry);
            List<GuildModel.BoxTaskInfo> info = r.ReadArray(ReadBoxTaskInfo);
            GuildModel.Instance.SetBoxInfo(num, maxNum, sendList, log, info);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_BOX_UPDATE);
            GameLog.Info("Guild", "40301 宝箱信息 num={0}/{1} send={2} log={3} info={4} remaining={5}B",
                num, maxNum, sendList.Count, log.Count, info.Count, r.Remaining);
        }

        private static GuildModel.BoxSendEntry ReadBoxSendEntry(NetReader r)
        {
            return new GuildModel.BoxSendEntry
            {
                AutoId = r.ReadU64(),
                RoleName = r.ReadString(),
                RoleId = r.ReadU64(),
                TaskId = (int)r.ReadU32(),
                Status = r.ReadU8(),
                Reward = GuildModel.ReadRewardList(r),
                Time = r.ReadU32(),
            };
        }

        private static GuildModel.BoxLogEntry ReadBoxLogEntry(NetReader r)
        {
            return new GuildModel.BoxLogEntry
            {
                RoleName = r.ReadString(),
                RoleId = r.ReadU64(),
                TaskId = (int)r.ReadU32(),
                Time = r.ReadU32(),
            };
        }

        private static GuildModel.BoxTaskInfo ReadBoxTaskInfo(NetReader r)
            => new GuildModel.BoxTaskInfo { TaskId = (int)r.ReadU32(), SendNum = r.ReadU8() };

        /// <summary>40302:code:i, send_list[u16×{auto_id:l(64位尾哨兵专测点),reward:ObjectList}]。
        /// 成功后本地摘除已领条目 + 补发40301刷新(对标老端 RefreshRewardBoxRed)。</summary>
        private void On40302(NetReader r)
        {
            int code = (int)r.ReadU32();
            List<(long autoId, List<GuildModel.RewardEntry> reward)> sendList =
                r.ReadArray(rr => (rr.ReadU64(), GuildModel.ReadRewardList(rr)));
            if (code == 1)
            {
                var ids = new List<long>(sendList.Count);
                foreach (var it in sendList) ids.Add(it.autoId);
                GuildModel.Instance.RemoveBoxEntries(ids);
                RequestBoxInfo();
                TipsManager.Toast("领取成功");
                EventDispatcher.Emit(GlobalEvent.EVT_GUILD_BOX_UPDATE);
                GameLog.Info("Guild", "40302 领取成功 count={0} remaining={1}B", sendList.Count, r.Remaining);
            }
            else
            {
                ShowError(code);
                GameLog.Info("Guild", "40302 领取失败 errorCode={0}", code);
            }
        }

        private void On40303(NetReader r)
        {
            List<GuildModel.BoxSendEntry> sendList = r.ReadArray(ReadBoxSendEntry);
            List<GuildModel.BoxLogEntry> log = r.ReadArray(ReadBoxLogEntry);
            GuildModel.Instance.AddBoxEntries(sendList, log);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_BOX_UPDATE);
            GameLog.Info("Guild", "40303 新宝箱推送(公会全员) send={0} log={1}", sendList.Count, log.Count);
        }

        private void On40304(NetReader r)
        {
            long autoId = r.ReadU64();
            GuildModel.Instance.RemoveBoxEntry(autoId);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_BOX_UPDATE);
            GameLog.Info("Guild", "40304 宝箱记录失效(公会全员) autoId={0}", autoId);
        }

        /// <summary>40305:**day_clear/gm_clear 触发时是 send_to_all 全服广播,不分公会**——recv 侧严禁假设
        /// 收到即代表自己有公会,纯按 TaskInfoList 内容 upsert,不触碰 GuildModel.Info/GuildId。</summary>
        private void On40305(NetReader r)
        {
            List<GuildModel.BoxTaskInfo> info = r.ReadArray(ReadBoxTaskInfo);
            GuildModel.Instance.ApplyBoxTaskInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_BOX_UPDATE);
            GameLog.Info("Guild", "40305 任务发放次数更新(可能是全服广播,不代表本公会/不刷红点) count={0}", info.Count);
        }

        // ==================== 结社协助(pt_404) ====================

        /// <summary>对标服务端 check_had_assist_other 的"5秒内刚发起过"分支——本地 CD 拦截,避免必然失败的重复请求。</summary>
        private long _lastAssistLaunchSec;

        /// <summary>发起协助请求(发 "chil" type[1boss/2副本/3璀璨之海/4主线本],sub_type,target_cfg_id,target_id)。
        /// **Guard**:本地5秒CD。</summary>
        public void LaunchAssist(int type, int subType, int targetCfgId, long targetId)
        {
            long now = TimeUtil.NowSec();
            if (now - _lastAssistLaunchSec < 5) { TipsManager.Toast("操作过于频繁"); return; }
            _lastAssistLaunchSec = now;
            SendFmt(Proto.GUILD_ASSIST_LAUNCH, "chil", type, subType, targetCfgId, targetId);
            GameLog.Info("Guild", "40401 发起协助 type={0} subType={1} targetId={2}", type, subType, targetId);
        }

        /// <summary>协助他人(发 "lc" assist_id,type——服务端业务层丢弃 Type,仅早期立即失败分支回显)。</summary>
        public void HelpAssist(long assistId, int type) => SendFmt(Proto.GUILD_ASSIST_HELP, "lc", assistId, type);

        /// <summary>取消协助/求助(发 "l" assist_id)。**Guard**:assistId&lt;=0 本地拦截。</summary>
        public void CancelAssist(long assistId)
        {
            if (assistId <= 0) return;
            SendFmt(Proto.GUILD_ASSIST_CANCEL, "l", assistId);
            GameLog.Info("Guild", "40403 取消协助 assistId={0}", assistId);
        }

        public void RequestAssistCount() => SendFmt(Proto.GUILD_ASSIST_COUNT);

        /// <summary>GuildHelpView 打开时拉取当日善缘进度(对标老端 LoadSuccess 发 40031)。</summary>
        public void RequestPrestigeDaily() => SendFmt(Proto.GUILD_PRESTIGE_DAILY);

        /// <summary>**Guard**:无公会时服务端 pp_guild_assist.erl:62-69 回 `send_to_sid(Sid, pt_404, 40405, [])`——
        /// 空实参列表匹配不上 pt_404.erl:81 `write(40405,[AssistList])` 的单元素模式,落到同文件 catch-all
        /// `write(_,_) -&gt; pt:pack(0, &lt;&lt;&gt;&gt;)`,客户端收到协议号 0 的空帧,等效于"永不回 40405"——
        /// 无公会时本地直接不发,避免调用方(未来 GuildHelpView)误以为会有回包而死等。</summary>
        public void RequestAssistList()
        {
            if (!GuildModel.IsHasGuild()) return;
            SendFmt(Proto.GUILD_ASSIST_LIST);
        }

        public void RequestMyAssist() => SendFmt(Proto.GUILD_ASSIST_MY_INFO);

        private void On40401(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long assistId = r.ReadU64();
            int type = r.ReadU8();
            int subType = r.ReadU16();
            int targetCfgId = (int)r.ReadU32();
            long targetId = r.ReadU64();
            if (errorCode == 1)
            {
                // 对标老端 SetReqData(scmd):落"我方求助"回显,供 On40403(isSelf)/On40407 命中后清空,
                // 避免协助 UI 接线时消费到"已取消却仍显示进行中"的脏数据。
                GuildModel.Instance.SetMyRequest(new GuildModel.MyAssistRequest
                {
                    AssistId = assistId, Type = type, SubType = subType, TargetCfgId = targetCfgId, TargetId = targetId,
                });
                TipsManager.Toast("已请求协助");
                GameLog.Info("Guild", "40401 发起协助成功 assistId={0} type={1}", assistId, type);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40401 发起协助失败 errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
        }

        /// <summary>40402:error_code:i, assist_id:l, type:c。成功且 type≠3(非璀璨之海)才补发40408
        /// (对标老端"落 AssistData")。</summary>
        private void On40402(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long assistId = r.ReadU64();
            int type = r.ReadU8();
            if (errorCode == 1)
            {
                if (type != 3) RequestMyAssist();
                TipsManager.Toast("正在前往协助"); // 文案对标老端(非"协助成功")
                GameLog.Info("Guild", "40402 协助成功 assistId={0} type={1}", assistId, type);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40402 协助失败 errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
        }

        /// <summary>40403:error_code:i, cancel_type:c[1主动/2璀璨之海结算触发], assist_id:l, ask_id:l。
        /// 按 ask_id 是否是自己区分"我取消了求助"(ReqData)vs"我取消了/对方取消了对某人的协助"(AssistData)——
        /// 老端后者是中性文案"已取消协助",不区分具体是谁触发的(对标老端)。</summary>
        private void On40403(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int cancelType = r.ReadU8();
            long assistId = r.ReadU64();
            long askId = r.ReadU64();
            if (errorCode == 1)
            {
                GuildModel.Instance.RemoveAssist(assistId);
                bool isSelf = askId == Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId;
                // 对标老端:isSelf→"我取消了自己的求助"清 ReqData;否则→"我正在协助的对象取消了求助"清 AssistData。
                // 按 assistId 命中而非无条件清空,防止晚到的旧包误清当前正在生效的另一条记录。
                if (isSelf)
                {
                    if (GuildModel.Instance.MyRequest != null && GuildModel.Instance.MyRequest.AssistId == assistId)
                        GuildModel.Instance.ClearMyRequest();
                }
                else if (GuildModel.Instance.CurrentMyAssist != null && GuildModel.Instance.CurrentMyAssist.AssistId == assistId)
                {
                    GuildModel.Instance.ClearMyAssist();
                }
                // 文案逐字对标老端(else 分支是中性"已取消协助",不区分"我方主动取消协助"与"对方取消了求助"——
                // 两种触发源共用同一提示,不要臆造"对方取消了对你的协助"这种更具体但老端没有的措辞)。
                TipsManager.Toast(isSelf ? "协助请求已取消" : "已取消协助");
                GameLog.Info("Guild", "40403 取消 assistId={0} cancelType={1} isSelf={2}", assistId, cancelType, isSelf);
            }
            else
            {
                ShowError(errorCode);
                GameLog.Info("Guild", "40403 取消失败 errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
        }

        /// <summary>40404:assist_count:c(**8位!**)。pp_guild_assist.erl:55-59 handle(40404,..) 无条件
        /// send_to_sid,**恒回包**(真静默的是 40408,见其注释)。</summary>
        private void On40404(NetReader r)
        {
            int count = r.ReadU8();
            GuildModel.Instance.SetAssistCount(count);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
            GameLog.Info("Guild", "40404 今日协助次数 count={0}", count);
        }

        /// <summary>40405:assist_list[u16×14字段](服务端全局 map 靠 GuildId 过滤,**无任何长度上限**,
        /// 客户端不做截断)。</summary>
        private void On40405(NetReader r)
        {
            List<GuildModel.AssistEntry> list = r.ReadArray(ReadAssistEntry);
            GuildModel.Instance.SetAssistList(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
            GameLog.Info("Guild", "40405 求助列表 count={0} remaining={1}B", list.Count, r.Remaining);
        }

        /// <summary>求助单条(item_to_bin_0,14字段;40405/40406 共用同一结构)。</summary>
        private static GuildModel.AssistEntry ReadAssistEntry(NetReader r)
        {
            var e = new GuildModel.AssistEntry
            {
                AssistId = r.ReadU64(),
                Type = r.ReadU8(),
                SubType = r.ReadU16(),
                TargetCfgId = (int)r.ReadU32(),
                TargetId = r.ReadU64(),
                RoleId = r.ReadU64(),
                Name = r.ReadString(),
                Level = r.ReadU16(),
                Career = r.ReadU8(),
                Sex = r.ReadU8(),
                Pic = r.ReadString(),
                PicVer = r.ReadU32(),
                IsAssist = r.ReadU8() != 0,
            };
            e.Extra = r.ReadArray(ReadAssistExtra);
            return e;
        }

        /// <summary>璀璨之海掠夺信息(item_to_bin_1/_2,7字段;仅 Type==3 时非空)。</summary>
        private static GuildModel.AssistExtra ReadAssistExtra(NetReader r)
        {
            return new GuildModel.AssistExtra
            {
                SerId = (int)r.ReadU32(),
                SerNum = r.ReadU16(),
                RoberId = r.ReadU64(),
                RoberName = r.ReadString(),
                RoberPower = r.ReadU32(),
                RoberReward = GuildModel.ReadRewardList(r),
                BackReward = GuildModel.ReadRewardList(r),
            };
        }

        private void On40406(NetReader r)
        {
            GuildModel.AssistEntry entry = ReadAssistEntry(r);
            GuildModel.Instance.UpsertAssist(entry);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
            GameLog.Info("Guild", "40406 新求助推送(公会全员) assistId={0} type={1}", entry.AssistId, entry.Type);
        }

        /// <summary>40407:assist_id:l。**扇出模式的一部分**(取消全部协助场景="1次本号广播+N次40403单播",
        /// 本号按条移除,不当全量刷新;recv 端逐条处理即为正确行为,无需额外识别是否属于扇出批次)。</summary>
        private void On40407(NetReader r)
        {
            long assistId = r.ReadU64();
            GuildModel.Instance.RemoveAssist(assistId);
            // 对标老端 on40407:adata/rdata 各自按 assist_id 命中才清空(同一 assistId 不可能同时是"我在帮的人"
            // 又是"我发的求助",两个 if 互不冲突,无需 else)。
            if (GuildModel.Instance.CurrentMyAssist != null && GuildModel.Instance.CurrentMyAssist.AssistId == assistId)
                GuildModel.Instance.ClearMyAssist();
            if (GuildModel.Instance.MyRequest != null && GuildModel.Instance.MyRequest.AssistId == assistId)
                GuildModel.Instance.ClearMyRequest();
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
            GameLog.Info("Guild", "40407 求助结束(公会全员) assistId={0}", assistId);
        }

        /// <summary>40408:12字段(比40405/40406单条少 is_assist/extra)。**Guard 静默说明**:请求本身在
        /// AssistId&gt;0 andalso AssistProcess==1 不满足(纯查询无进行中协助)时服务端真静默不回包
        /// (pp_guild_assist.erl:73-80 handle(40408,..) 不满足条件时 `_ -&gt; ok`),发送侧不能假设必有响应,
        /// 本轮不做等待超时兜底(与老端一致)。</summary>
        private void On40408(NetReader r)
        {
            var info = new GuildModel.MyAssistInfo
            {
                AssistId = r.ReadU64(),
                Type = r.ReadU8(),
                SubType = r.ReadU16(),
                TargetCfgId = (int)r.ReadU32(),
                TargetId = r.ReadU64(),
                RoleId = r.ReadU64(),
                Name = r.ReadString(),
                Level = r.ReadU16(),
                Career = r.ReadU8(),
                Sex = r.ReadU8(),
                Pic = r.ReadString(),
                PicVer = r.ReadU32(),
            };
            GuildModel.Instance.SetMyAssist(info);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
            GameLog.Info("Guild", "40408 当前协助对象 assistId={0}", info.AssistId);
        }

        private void On40409(NetReader r)
        {
            long assistId = r.ReadU64();
            TipsManager.Toast("协助成功");
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
            GameLog.Info("Guild", "40409 协助成功通知(面向协助者) assistId={0}", assistId);
        }

        private void On40410(NetReader r)
        {
            long assistId = r.ReadU64();
            long roleId = r.ReadU64();
            string name = r.ReadString();
            TipsManager.Toast(name + " 接受了你的协助请求");
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ASSIST_UPDATE);
            GameLog.Info("Guild", "40410 有人接受协助(面向求助者) assistId={0} roleId={1} name={2}", assistId, roleId, name);
        }

        // ==================== 结社武魂/神像(pt_405;per-player 数据,不做全公会广播) ====================

        public void RequestGodList() => SendFmt(Proto.GUILD_GOD_INFO);

        public void RequestGodRune(int godId) => SendFmt(Proto.GUILD_GOD_RUNE_INFO, "h", godId);

        public void UpgradeGodColor(int godId) => SendFmt(Proto.GUILD_GOD_COLOR_UP, "h", godId);

        public void AwakeGod(int godId) => SendFmt(Proto.GUILD_GOD_AWAKE, "h", godId);

        /// <summary>穿戴铭文(发 "hcl" god_id,pos_id,goods_id)。**DEAD 确认号**:服务端从不回 40505,
        /// 结果只能靠后续 40502 到达判断,调用方不要等待 40505 回调。**Guard**:pos_id∈[1,6](?pos_list)。</summary>
        public void WearGodRune(int godId, int posId, long goodsId)
        {
            if (posId < 1 || posId > 6) { GameLog.Warn("Guild", "40505 本地拦截:posId={0} 越界(合法[1,6])", posId); return; }
            SendFmt(Proto.GUILD_GOD_WEAR, "hcl", godId, posId, goodsId);
            GameLog.Info("Guild", "40505 穿戴铭文(DEAD确认号,靠40502判断结果) godId={0} pos={1}", godId, posId);
        }

        /// <summary>激活铭文组合(发 "hc" god_id,combo_id)。协议层设计上无 write 方向,结果靠 40502 判断。</summary>
        public void ActivateGodCombo(int godId, int comboId)
        {
            SendFmt(Proto.GUILD_GOD_COMBO_ACTIVATE, "hc", godId, comboId);
            GameLog.Info("Guild", "40506 激活组合(无write方向,靠40502判断结果) godId={0} comboId={1}", godId, comboId);
        }

        /// <summary>脱下铭文(发 "hc" god_id,pos)。**DEAD 确认号**,结果靠 40502 判断。**Guard**:pos∈[1,6]。</summary>
        public void TakeOffGodRune(int godId, int pos)
        {
            if (pos < 1 || pos > 6) { GameLog.Warn("Guild", "40507 本地拦截:pos={0} 越界(合法[1,6])", pos); return; }
            SendFmt(Proto.GUILD_GOD_TAKE_OFF, "hc", godId, pos);
            GameLog.Info("Guild", "40507 脱铭文(DEAD确认号,靠40502判断结果) godId={0} pos={1}", godId, pos);
        }

        /// <summary>升级铭文(发 "hc" god_id,pos)。**Guard**:pos∈[1,6]。</summary>
        public void UpgradeGodRune(int godId, int pos)
        {
            if (pos < 1 || pos > 6) { GameLog.Warn("Guild", "40508 本地拦截:pos={0} 越界(合法[1,6])", pos); return; }
            SendFmt(Proto.GUILD_GOD_RUNE_UPGRADE, "hc", godId, pos);
            GameLog.Info("Guild", "40508 升级铭文 godId={0} pos={1}", godId, pos);
        }

        /// <summary>激活铭文大师等级(发 "ch" god_id,lv——**GodId 8位独例,r13b 裁决:勿类推复用其余8个号
        /// 的16位解析函数**)。</summary>
        public void ActivateGodAchievement(int godId, int lv)
        {
            SendFmt(Proto.GUILD_GOD_ACHIEVEMENT_ACTIVATE, "ch", godId, lv);
            GameLog.Info("Guild", "40509 激活铭文大师等级 godId={0}(8位) lv={1}", godId, lv);
        }

        private void On40500(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode != 1) ShowError(errorCode); // 对标老端 `errcode != 1` 才显码,理论无害但照抄守卫
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_ERROR, errorCode);
            GameLog.Info("Guild", "40500 神像错误壳 errorCode={0}", errorCode);
        }

        /// <summary>40501:guild_title_lv:h, god_list[u16×{god_id:h,color:c,lv:h,god_power:l}]
        /// (遍历配置里**全部**神像id,未激活以{Id,0,0,0}占位,非"已拥有"列表)。</summary>
        private void On40501(NetReader r)
        {
            int guildTitleLv = r.ReadU16();
            List<GuildModel.GodEntry> list = r.ReadArray(rr => new GuildModel.GodEntry
            {
                GodId = rr.ReadU16(),
                Color = rr.ReadU8(),
                Lv = rr.ReadU16(),
                GodPower = rr.ReadU64(),
            });
            GuildModel.Instance.SetGodList(guildTitleLv, list);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_GOD_UPDATE);
            GameLog.Info("Guild", "40501 神像总览 guildTitleLv={0} count={1}", guildTitleLv, list.Count);
        }

        /// <summary>40502:god_id:h, rune_list[u16×{pos:c,goods_id:l,goods_type_id:i}](至多6条),
        /// combo_id:c, achievement_lvs[u16×{lv:h}], god_power:l。**本族万能刷新推送号**——40505/506/507/
        /// 508/509 五个操作成功后统一补发本号,不是各自独立确认(505/507还DEAD,连推都没有别的路)。</summary>
        private void On40502(NetReader r)
        {
            var detail = new GuildModel.GodDetail { GodId = r.ReadU16() };
            detail.RuneList.AddRange(r.ReadArray(rr => new GuildModel.GodRuneEntry
            {
                Pos = rr.ReadU8(),
                GoodsId = rr.ReadU64(),
                GoodsTypeId = (int)rr.ReadU32(),
            }));
            detail.ComboId = r.ReadU8();
            detail.AchievementLvs.AddRange(r.ReadArray(rr => (int)rr.ReadU16()));
            detail.GodPower = r.ReadU64();
            GuildModel.Instance.SetGodDetail(detail);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_GOD_UPDATE);
            GameLog.Info("Guild", "40502 神像铭文详情(万能刷新推送号) godId={0} runeCount={1} comboId={2}",
                detail.GodId, detail.RuneList.Count, detail.ComboId);
        }

        /// <summary>40503:god_id:h, color:c[升品后新品质], lv:h, god_power:l。</summary>
        private void On40503(NetReader r)
        {
            int godId = r.ReadU16();
            int color = r.ReadU8();
            int lv = r.ReadU16();
            long godPower = r.ReadU64();
            GuildModel.Instance.PatchGod(godId, color, lv, godPower);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_GOD_UPDATE);
            GameLog.Info("Guild", "40503 神像升品 godId={0} color={1}", godId, color);
        }

        /// <summary>40504:god_id:h, color:c[本次未变], lv:h[觉醒后新等级], god_power:l。
        /// **同一字段位置在40503/40504语义不同**(是否为本次操作变更值),消费方不要弄反。</summary>
        private void On40504(NetReader r)
        {
            int godId = r.ReadU16();
            int color = r.ReadU8();
            int lv = r.ReadU16();
            long godPower = r.ReadU64();
            GuildModel.Instance.PatchGod(godId, color, lv, godPower);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_GOD_UPDATE);
            GameLog.Info("Guild", "40504 神像觉醒 godId={0} lv={1}", godId, lv);
        }

        /// <summary>40508:code:i(真实存活,与DEAD的40505/507对照组;成功前服务端先推40502)。</summary>
        private void On40508(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code == 1) TipsManager.Toast("升级铭文成功");
            else ShowError(code);
            GameLog.Info("Guild", "40508 升级铭文 code={0}", code);
        }

        /// <summary>40509:code:i(成功前服务端先推40502;隐式门槛:6个铭文槽位须全部插满)。</summary>
        private void On40509(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code == 1) TipsManager.Toast("激活铭文大师等级成功");
            else ShowError(code);
            GameLog.Info("Guild", "40509 激活铭文大师等级 code={0}", code);
        }
    }
}
