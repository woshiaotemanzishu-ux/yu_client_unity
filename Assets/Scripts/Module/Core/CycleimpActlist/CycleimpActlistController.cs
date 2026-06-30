using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.CycleimpActlist
{
    /// <summary>
    /// 循环冲榜 / 竞榜协议(对标老端 CycleimpActlistController.On22700/22701/22702/22703,字节格式见 yu_server pt_227)。
    ///   22700 当前开启活动(GAME_START 拉一次):type/subtype 非0 → 拉 22701/22702/22703 并 Emit EVT_CYCLEIMP_DATA;=0 → Clear + EVT_CYCLEIMP_CLOSE。
    ///   22702 榜单 → 落榜首,Emit EVT_CYCLEIMP_DATA。22706 榜首变更推送 → 刷榜首。
    /// 主界面 _box_rank 的竞榜展示(3D模型+名次+倒计时)由 MainUIActivityView 监听 EVT_CYCLEIMP_DATA 渲染。
    /// 22701(个人信息)/22703(昨日榜)先只消费字节防错位,完整活动面板后续切片。
    /// </summary>
    public sealed class CycleimpActlistController : BaseController
    {
        public static readonly CycleimpActlistController Instance = new CycleimpActlistController();
        private CycleimpActlistController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.CYCLE_RANK_OPENING, On22700);
            RegisterProtocal(Proto.CYCLE_RANK_PANEL, On22701);
            RegisterProtocal(Proto.CYCLE_RANK_LIST, On22702);
            RegisterProtocal(Proto.CYCLE_RANK_YESTERDAY, On22703);
            RegisterProtocal(Proto.CYCLE_RANK_FIRST_CHANGE, On22706);
        }

        /// <summary>GAME_START 拉取当前开启的竞榜活动(对标老端 GAME_START 时 SendFmtToGame(22700))。无参。</summary>
        public void RequestStartup() => SendFmt(Proto.CYCLE_RANK_OPENING);

        // 22700: type:h, subtype:h, start_time:i, end_time:i, upon_end_time:i
        private void On22700(NetReader r)
        {
            int type = r.ReadU16();
            int subtype = r.ReadU16();
            long start = r.ReadU32();
            long end = r.ReadU32();
            long uponEnd = r.ReadU32();
            CycleimpActlistModel.Instance.SetActTime(type, subtype, start, end, uponEnd);

            if (type != 0 && subtype != 0)
            {
                GameLog.Info("CycleRank", "22700 活动开启 type={0} subtype={1} uponEnd={2}", type, subtype, uponEnd);
                SendFmt(Proto.CYCLE_RANK_PANEL, "hh", type, subtype);
                SendFmt(Proto.CYCLE_RANK_LIST, "hh", type, subtype);
                SendFmt(Proto.CYCLE_RANK_YESTERDAY);
                EventDispatcher.Emit(GlobalEvent.EVT_CYCLEIMP_DATA);
            }
            else
            {
                CycleimpActlistModel.Instance.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_CYCLEIMP_CLOSE);
            }
        }

        // 22701: type:h, subtype:h, is_open:c, score:i, rank:h, id:h, got_type:c —— 个人信息/红点用,主面板不依赖;先消费防错位。
        private void On22701(NetReader r)
        {
            r.ReadU16();  // type
            r.ReadU16();  // subtype
            r.ReadU8();   // is_open
            r.ReadU32();  // score
            r.ReadU16();  // rank
            r.ReadU16();  // id
            r.ReadU8();   // got_type
        }

        // 22702: type:h, subtype:h, score:i, rank:h, reward_id:h, len:h, items[{rank:h, server_id:i, role_id:l, role_name:s, role_score:i}]
        private void On22702(NetReader r)
        {
            r.ReadU16();          // type
            r.ReadU16();          // subtype
            r.ReadU32();          // my score
            r.ReadU16();          // my rank
            r.ReadU16();          // reward_id
            int n = r.ReadU16();
            var list = new List<CycleimpActlistModel.RankRoleVo>(n);
            for (int i = 0; i < n; i++) list.Add(ReadRankItem(r));
            CycleimpActlistModel.Instance.SetRankList(list);
            GameLog.Info("CycleRank", "22702 榜单 {0} 条,榜首={1}", n,
                CycleimpActlistModel.Instance.GetFirstHolder()?.RoleName ?? "<空>");
            EventDispatcher.Emit(GlobalEvent.EVT_CYCLEIMP_DATA);
        }

        // 22703: type:h, subtype:h, score:i, rank:h, push_type:c, len:h, items[...] —— 昨日榜弹窗用,主面板不依赖;先消费防错位。
        private void On22703(NetReader r)
        {
            r.ReadU16();  // type
            r.ReadU16();  // subtype
            r.ReadU32();  // score
            r.ReadU16();  // rank
            r.ReadU8();   // push_type
            int n = r.ReadU16();
            for (int i = 0; i < n; i++) ReadRankItem(r);
        }

        // 22706: rank_type:h, rank_subtype:h, server_id:i, role_id:l, role_name:s, role_score:i
        private void On22706(NetReader r)
        {
            r.ReadU16();  // rank_type
            r.ReadU16();  // rank_subtype
            var vo = new CycleimpActlistModel.RankRoleVo
            {
                Rank = 1,
                ServerId = (int)r.ReadU32(),
                RoleId = r.ReadU64(),
                RoleName = r.ReadString(),
                RoleScore = r.ReadU32(),
            };
            CycleimpActlistModel.Instance.SetFirstHolder(vo);
            EventDispatcher.Emit(GlobalEvent.EVT_CYCLEIMP_DATA);
        }

        private static CycleimpActlistModel.RankRoleVo ReadRankItem(NetReader r)
        {
            return new CycleimpActlistModel.RankRoleVo
            {
                Rank = r.ReadU16(),
                ServerId = (int)r.ReadU32(),
                RoleId = r.ReadU64(),
                RoleName = r.ReadString(),
                RoleScore = r.ReadU32(),
            };
        }
    }
}
