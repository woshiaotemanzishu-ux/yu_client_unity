using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.FriendInvite
{
    /// <summary>
    /// 好友邀请(分享)控制器(对标老客户端 FriendInviteController,协议段 34000~34012)。进游戏请求 34001→34012(3)→34005→34006;
    /// 据「分享开关」(FriendInviteModel.CheckIconOpenState)增删主界面图标 340。
    ///
    /// 图标条件对标老端 init_fun:CheckIconOpenState() 为真时 addIcon(340)——其内部再走 open_lv(30)/开服天/
    /// 审核隐藏 的配置门。本端 AddIconAsync 的 FunIsOpenByIconType 即等价的配置门,故用 AddIconAsync(而非
    /// AddOwnerIcon)。注:340 配置 controll_by_own_fun=true,默认图标扫描(RefreshDefaultIconsCoreAsync)会跳过它,
    /// 由本控制器独家管理,不会双管。等级变化(EVT_ROLE_INFO_UPDATE)时复走完整启动请求(对标老端 CHANGE_LEVEL→
    /// LevelChange→init_fun),让升到 30 级且分享开启后图标及时出现。
    ///
    /// 本期接图标、34005 帮助信息、34006 升级邀请角色及 34012 福利奖励状态全量快照；红点/助力/红包/福利面板/微信分享
    /// (34002~34004、34007~34011、11301/11302)与面板待用户验收。
    /// 轮22 族错误出口批补 34000(家族统一错误壳)。
    ///
    /// TODO(跨天 11301):老端 FriendInviteController.ts:156 在 DAY_CHANGE 时 SendFmtToGame(11301)
    /// (微信分享次数查询);本端 Proto 尚无 11301/11302 号、FriendInviteModel 亦无对应字段
    /// (UpdateShareTimes 等),移植该协议时再补回 DAY_CHANGE 订阅与 RequestXxx/OnXxx 实现——
    /// 空效果订阅(F3 裁决4)已移除,不在此保留零效果占位。
    /// </summary>
    public sealed class FriendInviteController : BaseController
    {
        public static readonly FriendInviteController Instance = new FriendInviteController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private FriendInviteController() { }

        public const string ICON_TYPE = FriendInviteModel.ICON_TYPE;

        // 复走完整启动请求的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.FRIENDINVITE_INFO, On34001);
            RegisterProtocal(Proto.FRIENDINVITE_ERROR, On34000);
            RegisterProtocal(Proto.FRIENDINVITE_LEVEL_INFO, On34006);
            RegisterProtocal(Proto.FRIENDINVITE_HELP_INFO, On34005);
            RegisterProtocal(Proto.FRIENDINVITE_WELFARE_INFO, On34012);
            // 对标老端 CHANGE_LEVEL→LevelChange→init_fun:等级变化时复请求(30级 + 分享开启后显示图标)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // DAY_CHANGE→11301 未接线,见类头 TODO(F3 裁决4:移除零效果订阅,不留空 handler)。
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            FriendInviteModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 init_fun 精确发送 34001→34012(3)→34005→34006)。</summary>
        public void RequestStartup()
        {
            SendEmpty(Proto.FRIENDINVITE_INFO);
            RequestWelfareInfo(FriendInviteModel.WelfareType);
            RequestHelpInfo();
            RequestLevelInfo();
        }

        public void RequestHelpInfo() => SendEmpty(Proto.FRIENDINVITE_HELP_INFO);
        public void RequestLevelInfo() => SendEmpty(Proto.FRIENDINVITE_LEVEL_INFO);
        public void RequestWelfareInfo(byte type)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.FRIENDINVITE_WELFARE_INFO, "c", type);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.FRIENDINVITE_WELFARE_INFO, "c", type);
        }

        private void SendEmpty(int proto)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(proto, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(proto);
        }

        // 34001: get_status:c, recover_time:i, daily_count:c, total_count:i, reward_list[u16×{reward_id:c, status:c}]
        private void On34001(NetReader r)
        {
            int getStatus = r.ReadU8();
            int recoverTime = (int)r.ReadU32();
            int dailyCount = r.ReadU8();
            int totalCount = (int)r.ReadU32();
            int rewardCount = r.ReadU16();
            for (int i = 0; i < rewardCount; i++)
            {
                r.ReadU8(); // reward_id
                r.ReadU8(); // status(奖励领取态,面板/红点用,本期不存)
            }

            FriendInviteModel m = FriendInviteModel.Instance;
            m.SetInfo(getStatus, recoverTime, dailyCount, totalCount);

            // 图标显隐取分享开关(CheckIconOpenState),等级/开服天/审核门由 AddIconAsync 配置门叠加把控。
            if (m.CheckIconOpenState()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("FriendInvite", "34001 好友邀请: get_status={0} daily={1} total={2} shareOpen={3}",
                getStatus, dailyCount, totalCount, m.CheckIconOpenState());
        }

        private void On34006(NetReader r)
        {
            List<FriendInviteModel.LevelInviteEntry> entries = r.ReadArray(ReadLevelEntry);
            FriendInviteModel.Instance.ReplaceLevelInfo(entries);
        }

        private void On34005(NetReader r)
        {
            ushort count = r.ReadU16();
            List<FriendInviteModel.RewardState> rewards = r.ReadArray(rr => new FriendInviteModel.RewardState { RewardId = rr.ReadU8(), Status = rr.ReadU8() });
            FriendInviteModel.Instance.ReplaceHelpInfo(count, rewards, r.ReadArray(ReadLevelEntry));
        }

        // 34012: type:u8, reward_list:u16×{reward_id:u8,status:u8}; each ordinary snapshot is isolated.
        private void On34012(NetReader r)
        {
            byte type = r.ReadU8();
            FriendInviteModel.Instance.ReplaceWelfareInfo(type,
                r.ReadArray(rr => new FriendInviteModel.RewardState { RewardId = rr.ReadU8(), Status = rr.ReadU8() }));
        }

        private static FriendInviteModel.LevelInviteEntry ReadLevelEntry(NetReader r) => new FriendInviteModel.LevelInviteEntry
        {
            InviteeId = unchecked((ulong)r.ReadU64()), Pos = r.ReadU8(), Name = r.ReadString(),
            Level = r.ReadU16(), Career = r.ReadU8(), Status = r.ReadU8()
        };

        // 对标老端:主角等级变化复走 init_fun(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        /// <summary>34000 好友邀请(pt_340)家族统一错误出口(对标老端 FriendInviteController.ts:160-163
        /// On34000:无条件 ErrorCodeShow(code,args)。服务端 send_error_code/3(lib_invite.erl:436-441)
        /// 是多处失败分支共享的错误壳,首字段 Pt 标识触发协议号,老端与本端均只读不透出)。
        /// 错误码表/args 格式化未移植,显码降级。</summary>
        private void On34000(NetReader r)
        {
            r.ReadU16();              // pt(触发协议号,老端 UI 不消费)
            int code = (int)r.ReadU32();
            string args = r.ReadString();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("FriendInvite", "34000 家族错误壳 pt-code={0} args={1}", code, args);
        }
    }
}
