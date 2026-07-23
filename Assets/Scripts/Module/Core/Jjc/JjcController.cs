using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Jjc
{
    /// <summary>
    /// 排位赛(竞技场 JJC)协议控制器(对标老端 ArenaController.ts;服务端 pt_280 段内 28001/28002/28003/28004)。
    /// 解主线 101465(ctype35「挑战对手」)。⚠服务端计数断链见 <see cref="JjcModel"/> 类注释——挑战本身能正常
    /// 发起并拿结果,但任务判定读的次数不会自然增长,需服务端修复 mod_jjc_cast.erl:87 后才能真正推进任务。
    /// </summary>
    public sealed class JjcController : BaseController
    {
        public static readonly JjcController Instance = new JjcController();
#if UNITY_EDITOR
        // CliVerify intercepts encoded empty requests without changing player send semantics.
        private static System.Func<byte[], bool> s_outboundIntercept;
#endif

        private JjcController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.JJC_INFO, On28001);
            RegisterProtocal(Proto.JJC_RIVALS, On28002);
            RegisterProtocal(Proto.JJC_CHALLENGE, On28003);
            RegisterProtocal(Proto.JJC_TIMES_INFO, On28004);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            JjcModel.Instance.Clear();
            base.Dispose();
        }

        private void OnGameStart()
        {
            JjcModel.Instance.Clear();
            RequestTimesInfo();
            RequestInfo();
        }

        /// <summary>请求页面信息(无参,对标老端 GAME_START 时发 28001)。</summary>
        public void RequestInfo()
        {
            SendEmpty(Proto.JJC_INFO);
            GameLog.Info("Jjc", "request 28001 jjc info");
        }

        /// <summary>请求随机对手(无参,对标老端 On28000 errcode 2800006/07 或打开面板时发 28002)。</summary>
        public void RequestRivals()
        {
            SendEmpty(Proto.JJC_RIVALS);
            GameLog.Info("Jjc", "request 28002 jjc rivals");
        }

        /// <summary>请求 28004 挑战次数完整快照(严格空包)。</summary>
        public void RequestTimesInfo()
        {
            SendEmpty(Proto.JJC_TIMES_INFO);
            GameLog.Info("Jjc", "request 28004 jjc times info");
        }

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
                if (s_outboundIntercept(frame)) return;
            }
#endif
            SendFmt(protoId);
        }

        /// <summary>挑战对手(发 "ilic" selfRank, rivalId, rivalRank, challengeType=0,对标
        /// ArenaController.ts:134 SendFmtToGame(28003,"ilic",args[0..3]))。</summary>
        public void Challenge(int selfRank, long rivalId, int rivalRank)
        {
            if (rivalId <= 0) return;
            SendFmt(Proto.JJC_CHALLENGE, "ilic", selfRank, rivalId, rivalRank, 0);
            GameLog.Info("Jjc", "challenge 28003 selfRank={0} rivalId={1} rivalRank={2}", selfRank, rivalId, rivalRank);
        }

        /// <summary>28001 页面信息:rank:i, history_rank:i, reward_rank:i, combat:l, hp:i, num:h, num_refresh:i,
        /// honour:i, is_reward:c, pet_id:i, break_id_list[u16×{break_id:i}]。字段序 1:1 摘自 ClientProtocol.json:2732。</summary>
        private void On28001(NetReader r)
        {
            int rank = (int)r.ReadU32();          // rank:i
            int historyRank = (int)r.ReadU32();    // history_rank:i
            int rewardRank = (int)r.ReadU32();     // reward_rank:i
            long combat = r.ReadU64();             // combat:l
            int hp = (int)r.ReadU32();             // hp:i
            int num = r.ReadU16();                 // num:h
            int numRefresh = (int)r.ReadU32();     // num_refresh:i
            int honour = (int)r.ReadU32();         // honour:i
            bool isReward = r.ReadU8() != 0;       // is_reward:c
            int petId = (int)r.ReadU32();          // pet_id:i
            List<int> breakIdList = r.ReadArray(rr => (int)rr.ReadU32());   // break_id_list[u16×{break_id:i}]

            JjcModel.Instance.Apply28001(rank, historyRank, rewardRank, combat, hp, num, numRefresh, honour, isReward, petId, breakIdList);
            GameLog.Info("Jjc", "28001 rank={0} num={1} honour={2} breakIds={3} remaining={4}B",
                rank, num, honour, breakIdList.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_JJC_UPDATE);
        }

        /// <summary>28002 随机对手:role_list[u16×{rank:i, role_id:l, combat:l, hp:i, pet_id:i, figure:RecFigure}]。
        /// figure 块用既有 <see cref="FigureProto"/>(与 12003/14200/登录/聊天等既有 RecFigure 读法同一套,字段全读完不截断)。</summary>
        private void On28002(NetReader r)
        {
            List<JjcModel.RivalVo> list = r.ReadArray(ReadRival28002);
            JjcModel.Instance.Apply28002(list);
            GameLog.Info("Jjc", "28002 role_list={0} remaining={1}B", list.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_JJC_UPDATE);
        }

        /// <summary>28003 挑战结果:role_list[u16×{role_id:l, figure:RecFigure, before_rank:h, rank:h, combat:l}],
        /// result:c, reward_list[u16×{type:c, type_id:i, num:l}](ObjectList形态,读完不留—未接奖励展示),
        /// break_reward_list:ObjectList(同形态,同样读完不留)。</summary>
        private void On28003(NetReader r)
        {
            List<JjcModel.RivalVo> roleList = r.ReadArray(ReadRival28003);
            int result = r.ReadU8();   // result:c
            r.ReadArray(rr => { rr.ReadU8(); rr.ReadU32(); rr.ReadU64(); return 0; });   // reward_list[u16×{type:c,type_id:i,num:l}](未接展示,按序读过)
            r.ReadArray(rr => { rr.ReadU8(); rr.ReadU32(); rr.ReadU64(); return 0; });   // break_reward_list:ObjectList(同形态,按序读过)

            JjcModel.Instance.Apply28003(result, roleList);
            TipsManager.Toast(result == 1 ? "挑战胜利" : "挑战失败");
            GameLog.Info("Jjc", "28003 result={0} roleList={1} remaining={2}B(⚠挑战次数计数服务端断链 mod_jjc_cast.erl:87,不推进主线任务)",
                result, roleList.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_JJC_UPDATE);

            // 老端 On28003 精确追发 28004 → 28002；28001 页面 slice 不在此处刷新。
            RequestTimesInfo();
            RequestRivals();
        }

        /// <summary>28004 次数完整快照: errcode:i32(32-bit wire 由 ReadU32 后 unchecked 转 int),
        /// left_num:u16, num_refresh:u32, can_buy_num:u16。</summary>
        private void On28004(NetReader r)
        {
            int errCode = unchecked((int)r.ReadU32());
            ushort leftNum = r.ReadU16();
            uint timesRefreshAt = r.ReadU32();
            ushort canBuyNum = r.ReadU16();
            JjcModel.Instance.Apply28004(errCode, leftNum, timesRefreshAt, canBuyNum);
            GameLog.Info("Jjc", "28004 err={0} left={1} refreshAt={2} canBuy={3} remaining={4}B", errCode, leftNum, timesRefreshAt, canBuyNum, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_JJC_UPDATE);
        }

        private static JjcModel.RivalVo ReadRival28002(NetReader r)
        {
            return new JjcModel.RivalVo
            {
                Rank = (int)r.ReadU32(),      // rank:i
                RoleId = r.ReadU64(),         // role_id:l
                Combat = r.ReadU64(),         // combat:l
                Hp = (int)r.ReadU32(),        // hp:i
                PetId = (int)r.ReadU32(),     // pet_id:i
                Figure = FigureProto.Read(r), // figure:RecFigure
            };
        }

        private static JjcModel.RivalVo ReadRival28003(NetReader r)
        {
            long roleId = r.ReadU64();          // role_id:l
            FigureProto figure = FigureProto.Read(r);   // figure:RecFigure
            r.ReadU16();                        // before_rank:h(未留字段,壳仅显示 rank)
            int rank = r.ReadU16();             // rank:h
            long combat = r.ReadU64();          // combat:l
            return new JjcModel.RivalVo { RoleId = roleId, Figure = figure, Rank = rank, Combat = combat };
        }
    }
}
