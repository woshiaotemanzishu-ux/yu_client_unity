using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.KfSingleRank
{
    public sealed class KfSingleRankController : BaseController
    {
        public static readonly KfSingleRankController Instance = new KfSingleRankController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private KfSingleRankController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.KF_SINGLE_RANK_INFO, On50701);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        private void OnGameStart()
        {
            KfSingleRankModel.Instance.Reset();
            RequestInfo();
        }

        public void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.KF_SINGLE_RANK_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.KF_SINGLE_RANK_INFO);
        }

        private void On50701(NetReader reader)
        {
            byte startLevel = reader.ReadU8();
            byte rewardState = reader.ReadU8();
            int count = reader.ReadU16();
            var levels = new List<KfSingleRankModel.LevelEntry>(count);
            for (int i = 0; i < count; i++)
            {
                levels.Add(new KfSingleRankModel.LevelEntry(reader.ReadU8(), reader.ReadU32()));
            }
            KfSingleRankModel.Instance.Replace(startLevel, rewardState, levels);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            KfSingleRankModel.Instance.Reset();
            base.Dispose();
        }
    }
}
