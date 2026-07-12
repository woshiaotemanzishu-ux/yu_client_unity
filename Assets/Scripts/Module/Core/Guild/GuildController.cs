using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

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
        }

        public override void Dispose()
        {
            GuildModel.Instance.Reset();
            base.Dispose();
        }

        private static void ShowError(int errorCode) => TipsManager.Toast("错误(" + errorCode + ")"); // 错误码表未移植,显码降级

        // ==================== 批量拉取(对标老端 RequestBaseInfo,本轮范围子集) ====================

        /// <summary>进入公会主界面时批量拉取(对标老端 GuildController.RequestBaseInfo,仅保留本轮范围内的号:
        /// 40005 基础信息/40021 权限/40023 捐献(数据层)/40040 技能(基础档)/40030 声望/40015 自身信息/
        /// 40061 合并候选;40231 守卫/40301 宝箱/40501 武魂/40405 协助/40101 仓库等留 13b)。</summary>
        public void RequestBaseInfo()
        {
            SendFmt(Proto.GUILD_BASE_INFO);
            SendFmt(Proto.GUILD_PERMISSION_LIST);
            SendFmt(Proto.GUILD_DONATE_INFO);
            SendFmt(Proto.GUILD_SKILL_LIST, "c", 1);
            SendFmt(Proto.GUILD_PRESTIGE_INFO);
            SendFmt(Proto.GUILD_SELF_INFO);
            SendFmt(Proto.GUILD_MERGE_LIST);
            GameLog.Info("Guild", "RequestBaseInfo 批量拉取(40005/21/23/40+40030/15/61)");
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
    }
}
