using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Compete
{
    /// <summary>
    /// 竞榜(赛事活动)控制器(对标老客户端 commonController/CompeteListController,协议段 338xx)。
    /// 进游戏请求 33800(拉正在开启的赛事活动列表);回包按每条活动解析图标类型
    /// 338@type@subtype 增删主界面图标(急速飞车/圣殿狮鹫/背饰竞榜… 100+ 种同一份列表驱动)。
    /// 老端 SetActList 对每条活动 addIcon(icon_type, buy_end_time),不在列表内的老图标删除;
    /// 本控制器用 _ownedIcons 做增量 diff(对标 CustomActivityController)。
    ///
    /// 33801/33802 只提供显式查询和独立 raw 快照，不恢复老端按活动自动扇出，也不接 UI/配置；
    /// 33803 仍由 CustomActivityController 持有，33804 领奖继续延期，33805/06/07 当前服务端发送链不可达。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求 33800(对标老端 CHANGE_LEVEL→game_star),
    /// 让升到活动开启等级后图标及时出现。
    /// </summary>
    public sealed class CompeteController : BaseController
    {
        public static readonly CompeteController Instance = new CompeteController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_readOutboundIntercept;
#endif
        private CompeteController() { }

        // 已由本控制器添加的竞榜图标(用于增量 diff:回包后删掉不再活跃的,添加新活跃的)。
        private readonly HashSet<string> _ownedIcons = new HashSet<string>();
        private int _applyVersion;

        // 复请求 33800 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.COMPETE_ACT_LIST, On33800);
            RegisterProtocal(Proto.COMPETE_VIEW_INFO, On33801);
            RegisterProtocal(Proto.COMPETE_RANK_INFO, On33802);
            // 对标老端 CHANGE_LEVEL→game_star:等级变化时复请求 33800。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // ServerClock(轮20 P4)补 DAY_CHANGE 复拉钩子(对标老端 CompeteListController.ts:206,
            // 绑定的是与 GAME_START 同一个 game_star 处理函数,见 OnServerDayChange 注释)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            ClearOwnedIcons();
            CompeteModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 game_star 发 33800)。</summary>
        public void RequestStartup()
        {
            SendFmt(Proto.COMPETE_ACT_LIST);
        }

        // 33800: act_list[u16×{type:h, subtype:h, show_id:h, start_time:i, end_time:i, buy_end_time:i}]
        private void On33800(NetReader r)
        {
            int count = r.ReadU16();
            var list = new List<CompeteModel.RaceActInfo>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new CompeteModel.RaceActInfo
                {
                    Type = r.ReadU16(),
                    Subtype = r.ReadU16(),
                    ShowId = r.ReadU16(),
                    StartTime = r.ReadU32(),
                    EndTime = r.ReadU32(),
                    BuyEndTime = r.ReadU32(),
                });
            }

            CompeteModel.Instance.SetActList(list);
            _ = ApplyActivityIconsAsync(list, ++_applyVersion);
            GameLog.Info("Compete", "33800 赛事活动列表: {0}", count);
        }

        /// <summary>显式查询指定活动的界面快照；请求无回包时保留同键旧值。</summary>
        public void RequestViewInfo(ushort type, ushort subtype)
        {
            SendReadRequest(Proto.COMPETE_VIEW_INFO, type, subtype);
        }

        /// <summary>显式查询指定活动的榜单快照；请求无回包时保留同键旧值。</summary>
        public void RequestRankInfo(ushort type, ushort subtype)
        {
            SendReadRequest(Proto.COMPETE_RANK_INFO, type, subtype);
        }

        private static void SendReadRequest(int protocol, ushort type, ushort subtype)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocol, "hh", new object[] { type, subtype });
            if (s_readOutboundIntercept != null && s_readOutboundIntercept(frame)) return;
#endif
            Instance.SendFmt(protocol, "hh", type, subtype);
        }

        private void On33801(NetReader r)
        {
            ushort type = r.ReadU16();
            ushort subtype = r.ReadU16();
            byte isOpen = r.ReadU8();
            uint score = r.ReadU32();
            uint todayScore = r.ReadU32();
            List<CompeteModel.ObjectEntry> cost = r.ReadArray(ReadObjectEntry);
            List<CompeteModel.ObjectEntry> tenCost = r.ReadArray(ReadObjectEntry);
            List<ushort> rewardIds = r.ReadArray(rr => rr.ReadU16());
            List<CompeteModel.StageEntry> stages = r.ReadArray(rr =>
                new CompeteModel.StageEntry(rr.ReadU16(), rr.ReadU8()));
            uint worldLevel = r.ReadU32();

            CompeteModel.Instance.ReplaceViewInfo(new CompeteModel.ViewSnapshot(
                type, subtype, isOpen, score, todayScore, cost, tenCost, rewardIds, stages, worldLevel));
        }

        private void On33802(NetReader r)
        {
            ushort type = r.ReadU16();
            ushort subtype = r.ReadU16();
            uint score = r.ReadU32();
            ushort rank = r.ReadU16();
            List<CompeteModel.RankEntry> entries = r.ReadArray(rr => new CompeteModel.RankEntry(
                rr.ReadU16(), rr.ReadU32(), unchecked((ulong)rr.ReadU64()), rr.ReadString(), rr.ReadU32()));

            CompeteModel.Instance.ReplaceRankInfo(
                new CompeteModel.RankSnapshot(type, subtype, score, rank, entries));
        }

        private static CompeteModel.ObjectEntry ReadObjectEntry(NetReader r)
        {
            return new CompeteModel.ObjectEntry(r.ReadU8(), r.ReadU32(), r.ReadU32());
        }

        // 对每条活动解析 338@type@subtype,增量增删图标(对标老端 SetActList + CustomActivityController.ApplyActivityIconsAsync)。
        private async Task ApplyActivityIconsAsync(List<CompeteModel.RaceActInfo> list, int version)
        {
            await MainUIConfigs.EnsureLoaded();

            // next: iconType -> buy_end_time(倒计时文本用)。只收 configfunctionicon 里真存在的键
            // (老端 act_type==0 无单独图标 → 该键没配 → 不进 next,等价原语义)。
            var next = new Dictionary<string, long>();
            for (int i = 0; i < list.Count; i++)
            {
                CompeteModel.RaceActInfo info = list[i];
                string iconType = CompeteModel.BuildIconType(info.Type, info.Subtype);
                if (MainUIConfigs.GetFunctionIconCfg(iconType) == null) continue;
                next[iconType] = info.BuyEndTime;
            }

            // 有更晚一次回包已发起时放弃本次(避免旧结果覆盖新状态)。
            if (version != _applyVersion) return;

            // 删除不再活跃(不在 next)的已有图标。
            var remove = new List<string>();
            foreach (string iconType in _ownedIcons)
            {
                if (!next.ContainsKey(iconType)) remove.Add(iconType);
            }
            for (int i = 0; i < remove.Count; i++)
            {
                _ownedIcons.Remove(remove[i]);
                ActivityIconManager.Instance.DeleteIcon(remove[i]);
            }

            // 添加/刷新当前活跃图标(AddIconAsync 内部再按 open_lv/open_day 门槛把关,不达标自动 no-op)。
            foreach (KeyValuePair<string, long> kv in next)
            {
                _ownedIcons.Add(kv.Key);
                await ActivityIconManager.Instance.AddIconAsync(kv.Key, kv.Value);
            }
        }

        private void ClearOwnedIcons()
        {
            foreach (string iconType in _ownedIcons)
            {
                ActivityIconManager.Instance.DeleteIcon(iconType);
            }
            _ownedIcons.Clear();
        }

        // 对标老端:主角等级变化复请求 33800(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        /// <summary>跨天(对标老端 CompeteListController.ts:206,绑定与 GAME_START 同一个 game_star 处理函数,
        /// ts:196-204):game_star 清空 actList/CompeteOpenActive/CompeteViewData/CompeteRankData/
        /// CompeteKeyData 五个字段；本端已建立 33801/33802 两份 raw 字典，其余抽奖/领奖/key data 不镜像
        /// + await InitConfig()(重载 confRaceActInfo
        /// 面板配置,本端同样未建,不镜像)+ 无条件重发 33800。本端对应清
        /// <see cref="CompeteModel.Reset"/>(清 actList,与老端 actList=[] 等价)+ 重发 33800。</summary>
        private void OnServerDayChange()
        {
            CompeteModel.Instance.Reset();
            RequestStartup();
        }
    }
}
