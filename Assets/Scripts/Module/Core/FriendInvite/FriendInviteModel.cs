using Shenxiao.Module.Core.Game;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.FriendInvite
{
    /// <summary>
    /// 好友邀请(分享)数据(对标老客户端 FriendInviteModel)。承载 34001 邀请基础信息、
    /// 34005 帮助信息、34006 升级邀请角色与 34012 福利奖励状态全量快照,并提供主界面图标(340)显隐判定。
    ///
    /// 图标开关铁律(对标老端 CheckIconOpenState = !is_alpha && ShareOpenState()):
    /// 好友邀请是「分享/邀请」社交入口,是否开启由**客户端构建/渠道配置**决定,而非服务端协议
    /// (34001 只承载领取进度,不决定图标显隐)。老端非微信包分支 ShareOpenState() = ClientConfig.share_open;
    /// 微信小游戏分支按 activity_cid(1/2/5/6 开)。**线上部署包(plat=shenhai_wanka)的
    /// ClientConfig.json 里 share_open=1**,故老端非微信分支该值为真、图标默认显示;本端为对齐线上原生包,
    /// <see cref="ShareOpen"/> 默认 true(落定线上值,同「等级门等 config 常量落定」做法)。
    /// 若后续接入 ClientConfig.json 解析,改由 login 引导按真实 share_open 赋值即可。
    /// 叠加的等级门(open_lv=30)/开服天/审核隐藏(need_hide_in_alpha)由 ActivityIconManager.AddIconAsync
    /// 的 FunIsOpenByIconType 统一把控(对标老端 addIcon 内部对 open_lv/open_day/need_hide_in_alpha 的校验)。
    /// 面板/助力/红包/福利等 UI 待用户验收,本期只做图标。
    /// </summary>
    public sealed class FriendInviteModel
    {
        public static readonly FriendInviteModel Instance = new FriendInviteModel();
        private FriendInviteModel() { }

        /// <summary>主界面图标类型(对标老端 FriendInviteModel.ICON_TYPE=340)。</summary>
        public const string ICON_TYPE = "340";

        /// <summary>
        /// 分享/邀请入口是否开启(对标老端 ClientConfig.share_open / 微信 activity_cid)。
        /// 线上部署包(shenhai_wanka)ClientConfig.share_open=1,故默认 true(图标随等级/开服天门显示)。
        /// 接入 ClientConfig.json 解析后,在登录引导里(主界面建活动图标之前)按真实分享开关赋值,
        /// 再复请求 34001 或 RefreshDefaultIconsAsync 重扫即可。
        /// (与 <see cref="PlatformModel.IsAlpha"/> 同为「平台/构建级」开关。)
        /// </summary>
        public static bool ShareOpen { get; set; } = true;

        // 34001 好友邀请基础信息(对标老端 On34001→SetInfo,面板/红点用,本期存下备用)
        public int GetStatus;   // 每日邀请奖励领取态(2=可领)
        public int RecoverTime; // 恢复/刷新时间戳
        public int DailyCount;  // 今日已邀请次数
        public int TotalCount;  // 累计邀请次数
        public sealed class LevelInviteEntry
        {
            public ulong InviteeId;
            public byte Pos;
            public string Name = "";
            public ushort Level;
            public byte Career;
            public byte Status;
        }
        public sealed class RewardState { public byte RewardId; public byte Status; }
        public const byte WelfareType = 3; // old FriendInviteModel.welfare_type
        public bool HasWelfareInfo { get; private set; }
        public byte WelfareInfoType { get; private set; }
        public readonly List<RewardState> WelfareRewards = new List<RewardState>();
        public bool HasLevelInfo { get; private set; }
        public readonly List<LevelInviteEntry> LevelInviteEntries = new List<LevelInviteEntry>();
        public bool HasHelpInfo { get; private set; }
        public ushort HelpCount { get; private set; }
        public readonly List<RewardState> HelpRewards = new List<RewardState>();
        public readonly List<LevelInviteEntry> HelpInviteEntries = new List<LevelInviteEntry>();

        public void SetInfo(int getStatus, int recoverTime, int dailyCount, int totalCount)
        {
            GetStatus = getStatus;
            RecoverTime = recoverTime;
            DailyCount = dailyCount;
            TotalCount = totalCount;
        }

        public void ReplaceLevelInfo(List<LevelInviteEntry> entries)
        {
            LevelInviteEntries.Clear();
            if (entries != null) LevelInviteEntries.AddRange(entries);
            HasLevelInfo = true;
        }
        public void ReplaceHelpInfo(ushort count, List<RewardState> rewards, List<LevelInviteEntry> entries)
        {
            HelpCount = count; HelpRewards.Clear(); HelpInviteEntries.Clear();
            if (rewards != null) HelpRewards.AddRange(rewards);
            if (entries != null) HelpInviteEntries.AddRange(entries);
            HasHelpInfo = true;
        }
        public void ReplaceWelfareInfo(byte type, List<RewardState> rewards)
        {
            WelfareInfoType = type;
            WelfareRewards.Clear();
            if (rewards != null) WelfareRewards.AddRange(rewards);
            HasWelfareInfo = true;
        }

        /// <summary>
        /// 图标入口开启状态(对标老端 CheckIconOpenState():!plat_form_mgr.is_alpha && ShareOpenState())。
        /// 审核态(IsAlpha)一律不显示;非审核态取分享开关(ShareOpen)。等级/开服天门由
        /// AddIconAsync 的配置门(open_lv=30)再叠加把控。
        /// </summary>
        public bool CheckIconOpenState()
        {
            return !PlatformModel.IsAlpha && ShareOpen;
        }

        public void Reset()
        {
            GetStatus = 0;
            RecoverTime = 0;
            DailyCount = 0;
            TotalCount = 0;
            LevelInviteEntries.Clear();
            HasLevelInfo = false;
            HelpCount = 0; HelpRewards.Clear(); HelpInviteEntries.Clear(); HasHelpInfo = false;
            WelfareInfoType = 0; WelfareRewards.Clear(); HasWelfareInfo = false;
        }
    }
}
