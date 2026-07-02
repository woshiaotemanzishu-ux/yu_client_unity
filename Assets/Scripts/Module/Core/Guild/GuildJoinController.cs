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

        /// <summary>40003 一键申请结果:error_code:i, guild_id:l, apply_type:c。
        /// code==1(或 guild_id>0)= 已申请/已加入(自动通过);否则显码降级(常见=已在公会/等级不足)。</summary>
        private void On40003(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long guildId = r.ReadU64();
            int applyType = r.ReadU8();
            if (errorCode == 1 || guildId > 0)
            {
                GuildJoinModel.Instance.MarkHasGuild(guildId > 0);
                TipsManager.Toast(applyType == 1 ? "已加入" : "已申请");
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
        /// 否则显码降级(常见=消耗不足/等级不足,建社消耗档位未确认)。</summary>
        private void On40004(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long guildId = r.ReadU64();
            if (errorCode == 1 && guildId > 0)
            {
                GuildJoinModel.Instance.MarkHasGuild(true);
                TipsManager.Toast("结社创建成功");
                GameLog.Info("Guild", "40004 create ok guildId={0}", guildId);
            }
            else
            {
                TipsManager.Toast("创建失败(" + errorCode + ")");   // 常见=消耗不足/等级不足
                GameLog.Info("Guild", "40004 create fail errorCode={0}", errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILD_UPDATE);
        }

        /// <summary>读 40001 guild_list 单项。</summary>
        private static GuildJoinModel.GuildBrief ReadGuildBrief(NetReader r)
        {
            var brief = new GuildJoinModel.GuildBrief();
            brief.GuildId = r.ReadU64();
            brief.Name = r.ReadString();
            brief.Lv = r.ReadU16();
            r.ReadU32();                 // guild_exp
            r.ReadU64();                 // chief_id
            r.ReadString();              // chief_name
            brief.MemberNum = r.ReadU16();
            brief.MemberCapacity = r.ReadU16();
            brief.IsApply = r.ReadU8() != 0;
            r.ReadU32();                 // auto_approve_power
            r.ReadU64();                 // combat_power
            r.ReadU8();                  // merge_status
            r.ReadU8();                  // is_master
            return brief;
        }
    }
}
