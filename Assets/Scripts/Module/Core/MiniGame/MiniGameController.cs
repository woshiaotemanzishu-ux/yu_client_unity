using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.MiniGame
{
    public sealed class MiniGameController : BaseController
    {
        public static readonly MiniGameController Instance = new MiniGameController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private MiniGameController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.MINI_GAME_ERROR, On39900);
            RegisterProtocal(Proto.MINI_GAME_START_NOTICE, On39901);
            RegisterProtocal(Proto.MINI_GAME_CURRENT, On39902);
            RegisterProtocal(Proto.MINI_GAME_RANK, On39904);
            RegisterProtocal(Proto.MINI_GAME_ELIM_RECONNECT, On39922);
        }

        /// <summary>对标老端 GAME_START 的断线状态探测；服务端无活动进程或写包失败时不会清旧快照。</summary>
        public void RequestStartup() => SendEmpty(Proto.MINI_GAME_CURRENT);

        /// <summary>显式查询一个小游戏模块的实时排行；请求本身不改变玩法状态。</summary>
        public void RequestRank(byte gameType, ushort moduleId, byte subId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MINI_GAME_RANK, "chc", new object[] { gameType, moduleId, subId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.MINI_GAME_RANK, "chc", gameType, moduleId, subId);
        }

        private void SendEmpty(int command)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(command, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(command);
        }

        private void On39900(NetReader reader)
        {
            MiniGameModel.Instance.ReplaceError(reader.ReadU32(), reader.ReadString());
        }

        private void On39901(NetReader reader)
        {
            uint code = reader.ReadU32();
            byte gameType = reader.ReadU8();
            ushort moduleId = reader.ReadU16();
            byte subId = reader.ReadU8();
            uint startTime = reader.ReadU32();
            uint endTime = reader.ReadU32();
            int count = reader.ReadU16();
            var info = new List<uint>(count);
            for (int i = 0; i < count; i++) info.Add(reader.ReadU32());
            MiniGameModel.Instance.ReplaceStartNotice(code, gameType, moduleId, subId, startTime, endTime, info);
        }

        private void On39902(NetReader reader)
        {
            byte gameType = reader.ReadU8();
            ushort moduleId = reader.ReadU16();
            byte subId = reader.ReadU8();
            uint startTime = reader.ReadU32();
            uint endTime = reader.ReadU32();
            int count = reader.ReadU16();
            var info = new List<uint>(count);
            for (int i = 0; i < count; i++) info.Add(reader.ReadU32());
            MiniGameModel.Instance.ReplaceCurrent(gameType, moduleId, subId, startTime, endTime, info);
        }

        private void On39904(NetReader reader)
        {
            byte gameType = reader.ReadU8();
            ushort moduleId = reader.ReadU16();
            byte subId = reader.ReadU8();
            int count = reader.ReadU16();
            var ranks = new List<MiniGameModel.RankEntry>(count);
            for (int i = 0; i < count; i++)
            {
                ranks.Add(new MiniGameModel.RankEntry(
                    reader.ReadU32(), reader.ReadU32(), reader.ReadU16(),
                    unchecked((ulong)reader.ReadU64()), reader.ReadString(), reader.ReadU32()));
            }
            MiniGameModel.Instance.ReplaceRank(gameType, moduleId, subId, ranks);
        }

        private void On39922(NetReader reader)
        {
            ushort moduleId = reader.ReadU16();
            byte subId = reader.ReadU8();
            uint startTime = reader.ReadU32();
            uint endTime = reader.ReadU32();
            uint score = reader.ReadU32();

            int boardCount = reader.ReadU16();
            var board = new List<MiniGameModel.BoardRow>(boardCount);
            for (int i = 0; i < boardCount; i++)
            {
                byte rowId = reader.ReadU8();
                int noteCount = reader.ReadU16();
                var notes = new List<byte>(noteCount);
                for (int j = 0; j < noteCount; j++) notes.Add(reader.ReadU8());
                board.Add(new MiniGameModel.BoardRow(rowId, notes));
            }

            int effectCount = reader.ReadU16();
            var effects = new List<MiniGameModel.EffectEntry>(effectCount);
            for (int i = 0; i < effectCount; i++)
                effects.Add(new MiniGameModel.EffectEntry(reader.ReadU8(), reader.ReadU8(), reader.ReadU8(), reader.ReadU8()));

            int scoreChessCount = reader.ReadU16();
            var scoreChess = new List<MiniGameModel.ScoreChessEntry>(scoreChessCount);
            for (int i = 0; i < scoreChessCount; i++)
                scoreChess.Add(new MiniGameModel.ScoreChessEntry(reader.ReadU8(), reader.ReadU8()));

            MiniGameModel.Instance.ReplaceElimReconnect(
                moduleId, subId, startTime, endTime, score, board, effects, scoreChess);
        }

        public override void Dispose()
        {
            MiniGameModel.Instance.Reset();
            base.Dispose();
        }
    }
}
