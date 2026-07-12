using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// 结社加入协议控制器(对标老端 GuildController.ts/GuildJoinView.ts;服务端 pt_400)。
    /// 不在 GameStart 主动拉(对标老端:仅打开结社面板时才发 40001);40004 创建=空服最短路径
    /// (建社成功 → 服务端 lib_guild_api common_join_guild → join_guild 事件 → 主线 101080/ctype14 完成)。
    /// 老端锚点:GuildJoinView.ts(打开发 shh 列表 + 30008 补触发)、DoTask case14 → GuildModel.OpenGuildView()。
    /// </summary>
    public sealed class GuildJoinController : BaseController
    {
        public static readonly GuildJoinController Instance = new GuildJoinController();

        private GuildJoinController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GUILD_LIST, On40001);
            RegisterProtocal(Proto.GUILD_APPLY_ONE, On40002);
            RegisterProtocal(Proto.GUILD_APPLY_ALL, On40003);
            RegisterProtocal(Proto.GUILD_CREATE, On40004);
        }

        public override void Dispose()
        {
            GuildJoinModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>结社列表(对标老端 GuildJoinView 打开时发 "shh" name="",pageSize=999,pageNo=1)。</summary>
        public void RequestList()
        {
            SendFmt(Proto.GUILD_LIST, "shh", "", 999, 1);
            GameLog.Info("Guild", "request 40001 guild list(对标 GuildJoinView 打开发包)");
        }

        /// <summary>一键批量申请加入(无参)。</summary>
        public void ApplyAll()
        {
            SendFmt(Proto.GUILD_APPLY_ALL);
            GameLog.Info("Guild", "apply 40003 apply all");
        }

        /// <summary>申请加入指定公会(发 "l" guild_id;对标老端 GuildListItem/TopItem 逐行"申请"按钮 40002,
        /// 区别于 40003 一键批量)。</summary>
        public void ApplyOne(long guildId)
        {
            if (guildId <= 0) return;
            SendFmt(Proto.GUILD_APPLY_ONE, "l", guildId);
            GameLog.Info("Guild", "apply 40002 guildId={0}", guildId);
        }

        /// <summary>创建结社(对标老端 "ls" cfgId=2, name;空服最短路径,建社消耗档位待确认,失败显码降级)。</summary>
        public void Create(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            SendFmt(Proto.GUILD_CREATE, "ls", 2, name);
            GameLog.Info("Guild", "create 40004 name={0}", name);
        }

        /// <summary>打开壳时补触发任务判定(对标老端 GuildJoinView.LoadSuccess 发 30008,无参)。</summary>
        public void NotifyTaskCheck()
        {
            SendFmt(Proto.CC_TASK_JOIN_GUILD);
            GameLog.Info("Guild", "notify 30008 join guild task check");
        }

        /// <summary>40001 列表:page_total:h, page_no:h, guild_list[u16×{guild_id:l, guild_name:s, guild_lv:h,
        /// guild_exp:i, chief_id:l, chief_name:s, member_num:h, member_capacity:h, is_apply:c,
        /// auto_approve_power:i, combat_power:l, merge_status:c, is_master:c}]。</summary>
        private void On40001(NetReader r)
        {
            int pageTotal = r.ReadU16();
            int pageNo = r.ReadU16();
            List<GuildJoinModel.GuildBrief> list = r.ReadArray(ReadGuildBrief);
            GuildJoinModel.Instance.SetList(list);
            GameLog.Info("Guild", "40001 pageTotal={0} pageNo={1} count={2} remaining={3}B",
                pageTotal, pageNo, list.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_UPDATE);
        }

        /// <summary>40002 单个申请结果:error_code:i, guild_id:l, apply_type:c(与 40003 同结构)。
        /// 对标老端 on40002:apply_type==1 已发出申请等待审批(guild_id 仍是目标公会 id,不代表已入会),
        /// ==2 自动审批直接入会;两者都补发 15718。
        /// **偏差(主动增强,非照抄老端)**:老端 on40002 本体从不写 RoleManager.guild_id(只有 on40003 才写,
        /// 靠后续服务端主动补发的 40015 间接补齐,这里是老端一处不对称的实现空隙);本实现仅在
        /// applyType==2(自动入会)且 guild_id>0 时才主动同步 RoleModel.GuildId,避免"刚点单个申请自动入会,
        /// HUD 公会图标仍判定无会"的短暂状态滞后——**严禁在 applyType==1(审批中)时同步**,因为服务端
        /// 40002 无论审批中还是自动入会都会回传目标 guild_id(非 0),若无条件同步会把"仅提交申请"的玩家
        /// 误判为已入会(HUD 会打开空的公会主界面)。与轮13a 新增 GuildController.On40015 的补发不冲突,
        /// 只是提前落值,偏差已记入工单 summary。</summary>
        private void On40002(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long guildId = r.ReadU64();
            int applyType = r.ReadU8();
            if (errorCode == 1)
            {
                if (applyType == 2 && guildId > 0) Shenxiao.Module.Core.Role.RoleModel.Instance.GuildId = guildId;
                GuildJoinModel.Instance.MarkHasGuild(applyType == 2 && guildId > 0);
                TipsManager.Toast(applyType == 2 ? "恭喜成功加入结社" : "已发出申请，请耐心等待");
                Shenxiao.Module.Core.Daily.DailyController.Instance.RequestSignUpList(); // 对标老端 SendFmtToGame(15718)
                GameLog.Info("Guild", "40002 apply ok guildId={0} applyType={1}", guildId, applyType);
            }
            else
            {
                TipsManager.Toast("申请失败(" + errorCode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("Guild", "40002 apply fail errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_UPDATE);
        }

        /// <summary>40003 一键申请结果:error_code:i, guild_id:l, apply_type:c。
        /// code==1(或 guild_id>0)= 已申请/已加入(自动通过,apply_type==2);否则显码降级(常见=已在公会/等级不足)。
        /// 对标老端 on40003:成功都补发 15718;guild_id==0(纯申请挂起,尚未真正加入)时额外重拉 40001 列表。</summary>
        private void On40003(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long guildId = r.ReadU64();
            int applyType = r.ReadU8();
            if (errorCode == 1 || guildId > 0)
            {
                if (guildId > 0) Shenxiao.Module.Core.Role.RoleModel.Instance.GuildId = guildId; // 对标老端 mainRoleVo.ChangeVar('guild_id',...)
                GuildJoinModel.Instance.MarkHasGuild(guildId > 0);
                TipsManager.Toast(applyType == 2 ? "已加入" : "已申请"); // applyType:1=审批挂起/2=自动入会(此前文案颠倒,已订正)
                Shenxiao.Module.Core.Daily.DailyController.Instance.RequestSignUpList(); // 对标老端 SendFmtToGame(15718)
                if (guildId == 0) RequestList(); // 对标老端:纯申请挂起时重拉 40001 列表刷新各行"已申请"态
                GameLog.Info("Guild", "40003 apply ok guildId={0} applyType={1}", guildId, applyType);
            }
            else
            {
                TipsManager.Toast("申请失败(" + errorCode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("Guild", "40003 apply fail errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_UPDATE);
        }

        /// <summary>40004 创建结果:error_code:i, guild_id:l。code==1 且 guild_id>0 → 建社成功(HasGuild=true);
        /// 否则显码降级(常见=消耗不足/等级不足,建社消耗档位未确认;该失败分支在服务端现状下走共享40000,
        /// 这里防御性无害,见 Proto.GUILD_ERROR 注释)。对标老端 on40004 成功补发 15718。</summary>
        private void On40004(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long guildId = r.ReadU64();
            if (errorCode == 1 && guildId > 0)
            {
                Shenxiao.Module.Core.Role.RoleModel.Instance.GuildId = guildId;
                Shenxiao.Module.Core.Role.RoleModel.Instance.GuildPosition = 1; // 建社者即会长(对标老端 ChangeVar('position',1))
                GuildJoinModel.Instance.MarkHasGuild(true);
                TipsManager.Toast("结社创建成功");
                Shenxiao.Module.Core.Daily.DailyController.Instance.RequestSignUpList(); // 对标老端 SendFmtToGame(15718)
                GameLog.Info("Guild", "40004 create ok guildId={0}", guildId);
            }
            else
            {
                TipsManager.Toast("创建失败(" + errorCode + ")");   // 常见=消耗不足/等级不足
                GameLog.Info("Guild", "40004 create fail errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_UPDATE);
        }

        /// <summary>读 40001 guild_list 单项(item_to_bin_0;40061 合并候选复用同结构)。
        /// guild_exp 字段服务端实为 gfunds(r13_server_pt400 §字段序陷阱#1),本工单范围暂不消费故仍丢弃;
        /// combat_power 字段实为前十战力和(combat_power_ten),见 GuildBrief.CombatPower 注释。</summary>
        private static GuildJoinModel.GuildBrief ReadGuildBrief(NetReader r)
        {
            var brief = new GuildJoinModel.GuildBrief();
            brief.GuildId = r.ReadU64();
            brief.Name = r.ReadString();
            brief.Lv = r.ReadU16();
            r.ReadU32();                 // guild_exp(实为 gfunds,本轮列表展示不需要)
            brief.ChiefId = r.ReadU64();
            brief.ChiefName = r.ReadString();
            brief.MemberNum = r.ReadU16();
            brief.MemberCapacity = r.ReadU16();
            brief.IsApply = r.ReadU8() != 0;
            brief.AutoApprovePower = r.ReadU32();
            brief.CombatPower = r.ReadU64();
            r.ReadU8();                  // merge_status
            r.ReadU8();                  // is_master(三态 MergeRel,本列表不需要)
            return brief;
        }
    }
}
