using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.DragonWhisper
{
    /// <summary>龙语秘境仅接管 65101 主面板快照，不附加启动、开放门或 UI 行为。</summary>
    public sealed class DragonWhisperController : BaseController
    {
        public static readonly DragonWhisperController Instance = new DragonWhisperController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private DragonWhisperController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.DRAGON_WHISPER_INFO, On65101);
        }

        /// <summary>显式拉取 65101；服务端无本号操作回包约定。</summary>
        public void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.DRAGON_WHISPER_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.DRAGON_WHISPER_INFO);
        }

        private void On65101(NetReader reader)
        {
            byte leftCount = reader.ReadU8();
            byte allCount = reader.ReadU8();
            var maps = reader.ReadArray(ReadMap);
            DragonWhisperModel.Instance.Replace(leftCount, allCount, maps);
        }

        private static DragonWhisperModel.MapEntry ReadMap(NetReader reader)
        {
            byte mapId = reader.ReadU8();
            ushort roleNum = reader.ReadU16();
            var monsters = reader.ReadArray(r => new DragonWhisperModel.MonsterEntry(r.ReadU32(), r.ReadU32()));
            return new DragonWhisperModel.MapEntry(mapId, roleNum, monsters);
        }

        public override void Dispose()
        {
            DragonWhisperModel.Instance.Reset();
            base.Dispose();
        }
    }
}
