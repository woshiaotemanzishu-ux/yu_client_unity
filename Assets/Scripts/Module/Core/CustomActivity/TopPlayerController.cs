using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// 头号玩家(TOP_PLAYER)控制器。对标老端 CustomActivityController.On22501/On22502 +
    /// MainUIActivityView 里 22501 的请求与 UPDATE_TOP_PLAYER_MAIN_DATA 派发。
    ///
    /// 职责:注册 22501/22502 回包写入 TopPlayerModel;在「开服天<=8 且 等级>=130」时按 config_rush_rank
    /// 时间窗请求 22501;收到数据且仍在窗口内 → 经 ActivityIconManager.AddOwnerIcon 强加 331@10@0
    /// (绕过 open_lv=999 的通用门),并 Emit EVT_TOPPLAYER_MAIN_DATA 让活动视图挂 ui_cb01 + 填文案。
    ///
    /// 仍缺(留作后续):奖励领取 22503/22504、get-way 22505、榜单 reward item + 3D 模型、逐秒倒计时、
    /// 红点(22502 goal_list 已读取但未驱动 _img_red)。需要 config_rush_rank.json 与 331_10.png 导入为
    /// Addressable 后此图标才会真正出现。
    /// </summary>
    public sealed class TopPlayerController : BaseController
    {
        public static readonly TopPlayerController Instance = new TopPlayerController();
        private TopPlayerController() { }

        private bool _requested;

        protected override void Register()
        {
            RegisterProtocal(Proto.TOP_PLAYER_RANK_INFO, On22501);   // 22501
            RegisterProtocal(Proto.TOP_PLAYER_GOAL_INFO, On22502);   // 22502
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_READY, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_READY, OnRoleInfoUpdate);
            TopPlayerModel.Instance.Reset();
            _requested = false;
            base.Dispose();
        }

        private void OnRoleInfoUpdate()
        {
            if (_requested) return;
            if (!GatePasses()) return;
            _requested = true;
            _ = RequestOpenRanksAsync();
        }

        /// <summary>对标 MainUIActivityView.InitEvent: open_day<=8 && lv>=130。</summary>
        public static bool GatePasses()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return false;
            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;
            return openDay <= 8 && role.Level >= 130;
        }

        /// <summary>对标 MainUIActivityView.InitEvent 末尾的 config_rush_rank 遍历 + 22501("ih")。</summary>
        private async Task RequestOpenRanksAsync()
        {
            await RushRankConfigs.EnsureLoaded();
            if (RushRankConfigs.All == null) return;

            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;
            long openTime = ServerTimeModel.OpenTime; // = 老端 GetOpenServerTime()

            foreach (KeyValuePair<int, RushRankConfigs.RushRankCfg> kv in RushRankConfigs.All)
            {
                RushRankConfigs.RushRankCfg v = kv.Value;
                if (v.StartDay <= openDay && openDay <= v.ClearDay
                    && openTime >= v.OpenStartTime && openTime <= v.OpenEndTime)
                {
                    // 老端: SendFmtToGame(22501, "ih", rank_type(k), 1)
                    SendFmt(Proto.TOP_PLAYER_RANK_INFO, "ih", kv.Key, 1);
                }
            }
        }

        // 22501: rank_type:i, sel_rank:i, sel_val:l, sum:i, max_len:h, rank_limit:i, status:c, end_time:l,
        //        is_combat:c, rank_list[u16 × {player_id:l, name:s, first_value:l, rank:i}]
        private void On22501(NetReader r)
        {
            var info = new TopPlayerModel.RankInfo
            {
                RankType = (int)r.ReadU32(),
            };
            r.ReadU32();                 // sel_rank
            r.ReadU64();                 // sel_val
            r.ReadU32();                 // sum
            r.ReadU16();                 // max_len
            r.ReadU32();                 // rank_limit
            info.Status = r.ReadU8();
            info.EndTime = r.ReadU64();
            info.IsCombat = r.ReadU8();
            int n = r.ReadU16();
            for (int i = 0; i < n; i++)
            {
                info.RankList.Add(new TopPlayerModel.RankRoleVo
                {
                    PlayerId = r.ReadU64(),
                    Name = r.ReadString(),
                    FirstValue = r.ReadU64(),
                    Rank = (int)r.ReadU32(),
                });
            }

            TopPlayerModel.Instance.SetRankInfo(info);
            GameLog.Info("TopPlayer", "22501 rank_type={0} end_time={1} first='{2}'",
                info.RankType, info.EndTime, info.FirstName());
            _ = OnRankDataReadyAsync(info);
        }

        // 22502: goal_list[u16 × {rank_type:i, goal[u16 × {goalId:l, status:c}]}] —— 仅红点用;
        // 主界面展示不依赖,这里读完不报错即可(防止后续协议错位)。
        private void On22502(NetReader r)
        {
            int n = r.ReadU16();
            for (int i = 0; i < n; i++)
            {
                r.ReadU32(); // rank_type
                int gn = r.ReadU16();
                for (int g = 0; g < gn; g++)
                {
                    r.ReadU64(); // goalId
                    r.ReadU8();  // status
                }
            }
        }

        /// <summary>对标老端 On22501 末尾的 config_rush_rank 时间窗判定 + UPDATE_TOP_PLAYER_MAIN_DATA。</summary>
        private async Task OnRankDataReadyAsync(TopPlayerModel.RankInfo info)
        {
            if (!GatePasses()) return;
            await RushRankConfigs.EnsureLoaded();

            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;
            long openTime = ServerTimeModel.OpenTime;

            RushRankConfigs.RushRankCfg v = RushRankConfigs.Get(info.RankType);
            bool inWindow = v != null
                && v.StartDay <= openDay
                && openTime >= v.OpenStartTime && openTime <= v.OpenEndTime;
            if (!inWindow) return;

            await MainUIConfigs.EnsureLoaded();
            // 强加 331@10@0,绕过 open_lv=999/controll_by_own_fun 的通用门。
            ActivityIconManager.Instance.AddOwnerIcon(TopPlayerModel.ICON_TYPE);
            // 通知活动视图:挂 ui_cb01、填榜首名/活动名。
            EventDispatcher.Emit(GlobalEvent.EVT_TOPPLAYER_MAIN_DATA, info.RankType);
        }
    }
}
