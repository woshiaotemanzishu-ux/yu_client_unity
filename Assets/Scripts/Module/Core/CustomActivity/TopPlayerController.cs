using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
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
    /// 仍缺(留作后续):榜单 reward item + 3D 模型、逐秒倒计时。入口红点由 22501 排名奖励状态和
    /// 22502 目标奖励状态统一驱动。
    ///
    /// 【自动循环 轮17 P6 新增】22500(通用错误码)/22503(领目标奖)/22504(领排名奖)/22505(获取途径)。
    /// wire 全部回 pt_225.erl 原文核(read:8-27行,write:30-129行,item_to_bin_0/1/2/3:134-184行)。
    /// 任务只放行本文件(TopPlayerController.cs)一个文件,TopPlayerModel.cs 不在可写范围——22503/22504 的
    /// "成功后重拉 22502"联动改走直接 SendFmt(对标老端 Fire(SCMD_REQUEST,22502,sub_type),ts:1195/1208);
    /// 22504 的 SetActResult(scmd) 落地复用 P1 已提供的公开 API CustomActivityModel.Instance.SetClaimResult
    /// (baseType=TopPlayerModel.ACT_BASE_TYPE);22505 的 GetWay 数据没有 Model 可落,改存本类私有字段
    /// (_getWayByRushId),Emit 复用既有通用事件 EVT_CUSTOMACT_DETAIL_UPDATE 替代老端 UPDATE_VIEW 语义。
    ///
    /// 【C2S 参数序订正,已回填 Proto.cs】经 TopPlayerItem.ts:50-52 实际调用点 Fire(SCMD_REQUEST,22504,
    /// rank_type,1,id) 与 pt_225.erl:15-24 read(22503)/read(22504) 逐字段核对,真实顺序是 Type,
    /// SubType(固定值1),Goal/RewardId(fmt "ihc" 对应 i=Type,h=SubType,c=Goal/RewardId);"1"是硬编码的
    /// TopPlayerModel.ACT_SUB_TYPE。本文件按 erl 原文实现;Proto.cs TOP_PLAYER_GOAL_CLAIM/TOP_PLAYER_RANK_CLAIM
    /// 注释已同步订正(不再是早期误记的 "Type,Goal,SubType")。
    /// </summary>
    public sealed class TopPlayerController : BaseController
    {
        public static readonly TopPlayerController Instance = new TopPlayerController();
        private TopPlayerController() { }

        private bool _requested;

        /// <summary>22505 获取途径信息缓存(对标老端 model.TopPlayerGetWayInfo(scmd);TopPlayerModel.cs 不在
        /// 本轮可写文件范围,暂存本类私有字段,key=RushId)。</summary>
        private readonly Dictionary<int, List<GetWayEntry>> _getWayByRushId = new Dictionary<int, List<GetWayEntry>>();

        /// <summary>获取途径单条(对标 pt_225.erl item_to_bin_3:174-184)。</summary>
        public sealed class GetWayEntry
        {
            public int JumpId;
            public int Label;
            public long EndTime;
        }

        private static void ShowError(int errorCode) => TipsManager.Toast("错误(" + errorCode + ")"); // 错误码表未移植,显码降级(仿 CustomActivityController.Core.cs)

        protected override void Register()
        {
            RegisterProtocal(Proto.TOP_PLAYER_RANK_INFO, On22501);   // 22501
            RegisterProtocal(Proto.TOP_PLAYER_GOAL_INFO, On22502);   // 22502
            RegisterProtocal(Proto.TOP_PLAYER_ERROR, On22500);       // 22500(P6新增)
            RegisterProtocal(Proto.TOP_PLAYER_GOAL_CLAIM, On22503);  // 22503(P6新增)
            RegisterProtocal(Proto.TOP_PLAYER_RANK_CLAIM, On22504);  // 22504(P6新增)
            RegisterProtocal(Proto.TOP_PLAYER_GET_WAY, On22505);     // 22505(P6新增)
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_READY, OnRoleInfoUpdate);
            // ServerClock(轮20 P4)补 DAY_CHANGE 复拉钩子(对标老端 topPlayer/TopPlayerView.ts:212
            // change_day→UpdateTab,其函数体对每个 start_day<=open_day 的已开榜 tab 发 22501)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_READY, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            ActivityIconManager.Instance.SetIconRedDot(TopPlayerModel.ICON_TYPE, false);
            TopPlayerModel.Instance.Reset();
            _requested = false;
            _getWayByRushId.Clear();
            base.Dispose();
        }

        /// <summary>跨天(对标老端 TopPlayerView.ts:208-212 change_day→UpdateTab):UpdateTab 按
        /// start_day&lt;=open_day 遍历已开榜的 tab,逐个 Fire(SCMD_REQUEST,22501,type,sub_type)(ts:274-286)
        /// ——与既有 <see cref="RequestOpenRanksAsync"/>(config_rush_rank 时间窗遍历发 22501)是同一段逻辑的
        /// 另一处触发点,直接复用,不额外加 GatePasses/_requested 门槛(老端 UpdateTab 本身无这两道门,
        /// 那是 OnRoleInfoUpdate 首次拉取专属的去抖,不适用于这里)。
        /// ⚠TopPkController.ts:106(281xx 巅峰对决系统)的同名 DAY_CHANGE 绑定不在本方法镜像范围——
        /// 本仓尚无 TopPk/281xx 对应 Controller/Model(零 28101-28107 注册),按 spec §3.5"无对应模块不许
        /// 新建"不接,留后续轮次。</summary>
        private void OnServerDayChange()
        {
            _ = RequestOpenRanksAsync();
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

        /// <summary>对标 MainUIActivityView.InitEvent 末尾的 config_rush_rank 遍历 + 22501("ih")。改 public
        /// (自动循环 轮17 B2):供 CustomActivityController.Core.cs 的 RequestActDetail TOP_PLAYER 分支
        /// 直接调用,镜像老端 RequireActInfo TOP_PLAYER 分支里紧跟 22502 之后的同一段遍历(无角色等级/开服
        /// 天数门禁,与 OnRoleInfoUpdate 那条带门禁的触发路径语义不同,两者并存不冲突)。</summary>
        public async Task RequestOpenRanksAsync()
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
            RefreshRedDot();
            GameLog.Info("TopPlayer", "22501 rank_type={0} end_time={1} first='{2}'",
                info.RankType, info.EndTime, info.FirstName());
            _ = OnRankDataReadyAsync(info);
        }

        // 22502: goal_list[u16 × {rank_type:i, goal[u16 × {goalId:l, status:c}]}]，整包替换目标红点状态。
        private void On22502(NetReader r)
        {
            int n = r.ReadU16();
            bool hasRed = false;
            for (int i = 0; i < n; i++)
            {
                r.ReadU32(); // rank_type
                int gn = r.ReadU16();
                for (int g = 0; g < gn; g++)
                {
                    r.ReadU64(); // goalId
                    if (r.ReadU8() == 1) hasRed = true;
                }
            }
            TopPlayerModel.Instance.SetGoalRed(hasRed);
            RefreshRedDot();
        }

        private static void RefreshRedDot()
        {
            ActivityIconManager.Instance.SetIconRedDot(
                TopPlayerModel.ICON_TYPE, TopPlayerModel.Instance.HasEntranceRedDot());
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

        // ---------------------------------------------------------------------------------------
        // 自动循环 轮17 P6 新增:22500/22503/22504/22505(wire 全部回 pt_225.erl 原文核,见头注释)。
        // ---------------------------------------------------------------------------------------

        /// <summary>22500 头号玩家通用错误码(S2C only,pt_225.erl:30-36 write;无 read,C2S 侧无此号)。
        /// 对标老端 On22500(ts:1122-1127):仅 error_code!=1012 弹窗。</summary>
        private void On22500(NetReader r)
        {
            int code = r.ReadI32();
            if (code != 1012) ShowError(code);
            GameLog.Info("TopPlayer", "22500 通用错误码 code={0}", code);
        }

        /// <summary>22503 领取目标奖励回执:ErrorCode:32,Type:32,Goal:8,SubType:16(pt_225.erl:86-98 write)。
        /// 对标老端 On22503(ts:1190-1197):**仅 code==1 时**重拉 22502 刷新目标列表;失败老端无 else 分支,
        /// 不弹错也不刷新(照抄这个不对称行为,不额外加 ShowError)。</summary>
        private void On22503(NetReader r)
        {
            int code = r.ReadI32();
            int type = (int)r.ReadU32();
            int goal = r.ReadU8();
            int subType = r.ReadU16();
            if (code == 1)
            {
                SendFmt(Proto.TOP_PLAYER_GOAL_INFO, "h", subType); // 对标 Fire(SCMD_REQUEST,22502,sub_type),ts:1195
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, TopPlayerModel.ACT_BASE_TYPE, subType, code);
            GameLog.Info("TopPlayer", "22503 领取目标奖励回执 code={0} type={1} goal={2} sub={3}", code, type, goal, subType);
        }

        /// <summary>22504 领取排名奖励回执:ErrorCode:32,RewardId:8,SubType:16,Type:32(pt_225.erl:100-112 write)。
        /// 对标老端 On22504(ts:1199-1209):**无论成败**都 SetActResult(scmd) + 重拉 22502(与 22503 不同,
        /// 22503 只在成功时才刷新)。SetActResult 落地复用 CustomActivityModel 既有公开 API SetClaimResult。
        /// **三镜头订正,去掉老端没有的弹码**:老端 On22504 全函数体没有任何 ShowError/Util.ErrorCodeShow
        /// 调用(失败静默,不弹窗;失败信息走独立的 22500 通用错误码通道,这里再弹一次会双弹),已删除
        /// 原先误加的 `if(code!=1) ShowError(code)`,保留 GameLog+EVT_CUSTOMACT_RESULT 事件留痕成败。</summary>
        private void On22504(NetReader r)
        {
            int code = r.ReadI32();
            int rewardId = r.ReadU8();
            int subType = r.ReadU16();
            int type = (int)r.ReadU32();
            CustomActivityModel.Instance.SetClaimResult(TopPlayerModel.ACT_BASE_TYPE, subType, rewardId, code);
            SendFmt(Proto.TOP_PLAYER_GOAL_INFO, "h", subType); // 对标 Fire(SCMD_REQUEST,22502,sub_type),ts:1208,无条件执行
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, TopPlayerModel.ACT_BASE_TYPE, subType, code);
            GameLog.Info("TopPlayer", "22504 领取排名奖励回执 code={0} rewardId={1} sub={2} type={3}", code, rewardId, subType, type);
        }

        /// <summary>22505 获取途径信息:RushId:32,Res[u16计数]×{JumpId:32,Label:32,EndTime:64}
        /// (pt_225.erl:114-129 write,item_to_bin_3:174-184)。对标老端 On22505(ts:1210-1214):
        /// model.TopPlayerGetWayInfo(scmd) + Fire(UPDATE_VIEW,10,1,rush_id)——TopPlayerModel.cs 不在本轮可写
        /// 文件范围,数据暂存本类私有字段 _getWayByRushId,Emit 复用既有通用事件 EVT_CUSTOMACT_DETAIL_UPDATE
        /// (base=10,sub=1)替代 UPDATE_VIEW 语义。</summary>
        private void On22505(NetReader r)
        {
            int rushId = (int)r.ReadU32();
            List<GetWayEntry> list = r.ReadArray(rr => new GetWayEntry
            {
                JumpId = (int)rr.ReadU32(),
                Label = (int)rr.ReadU32(),
                EndTime = rr.ReadU64(),
            });
            _getWayByRushId[rushId] = list;
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, TopPlayerModel.ACT_BASE_TYPE, TopPlayerModel.ACT_SUB_TYPE);
            GameLog.Info("TopPlayer", "22505 获取途径信息 rushId={0} resN={1}", rushId, list.Count);
        }

        public IReadOnlyList<GetWayEntry> GetGetWay(int rushId) =>
            _getWayByRushId.TryGetValue(rushId, out List<GetWayEntry> v) ? v : null;

        /// <summary>22503 领取目标奖励(C2S "ihc" Type,SubType,Goal;SubType 固定 TopPlayerModel.ACT_SUB_TYPE,
        /// 对标 TopPlayerItem.ts:52 `Fire(SCMD_REQUEST,22503,rank_type,1,id)`,顺序订正见头注释)。</summary>
        public void RequestGoalClaim(int type, int goal) =>
            SendFmt(Proto.TOP_PLAYER_GOAL_CLAIM, "ihc", type, TopPlayerModel.ACT_SUB_TYPE, goal);

        /// <summary>22504 领取排名奖励(C2S "ihc" Type,SubType,RewardId,同上订正;对标 TopPlayerItem.ts:50)。</summary>
        public void RequestRankClaim(int type, int rewardId) =>
            SendFmt(Proto.TOP_PLAYER_RANK_CLAIM, "ihc", type, TopPlayerModel.ACT_SUB_TYPE, rewardId);

        /// <summary>22505 获取途径(C2S "i" RushId,对标 ts:403-404)。</summary>
        public void RequestGetWay(int rushId) =>
            SendFmt(Proto.TOP_PLAYER_GET_WAY, "i", rushId);
    }
}
