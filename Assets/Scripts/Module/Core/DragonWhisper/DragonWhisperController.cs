using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.DragonWhisper
{
    /// <summary>龙语秘境当前接管 65100 共享错误、65101 主面板快照与 65106 掉落记录，
    /// 不附加启动、开放门或 UI 行为。</summary>
    public sealed class DragonWhisperController : BaseController
    {
        public static readonly DragonWhisperController Instance = new DragonWhisperController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept = null;
#endif

        private DragonWhisperController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.DRAGON_WHISPER_ERROR, On65100);
            RegisterProtocal(Proto.DRAGON_WHISPER_INFO, On65101);
            RegisterProtocal(Proto.DRAGON_WHISPER_DROP_LOG, On65106);
        }

        private void On65100(NetReader reader)
        {
            DragonWhisperModel.Instance.ReplaceError(reader.ReadU32());
        }

        /// <summary>显式拉取 65101；服务端无本号操作回包约定。</summary>
        public void RequestInfo()
        {
            SendEmpty(Proto.DRAGON_WHISPER_INFO);
        }

        /// <summary>显式拉取 65106 完整掉落记录快照。</summary>
        public void RequestDropLog()
        {
            SendEmpty(Proto.DRAGON_WHISPER_DROP_LOG);
        }

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId);
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

        private void On65106(NetReader reader)
        {
            DragonWhisperModel.Instance.ReplaceDropLog(reader.ReadArray(ReadDropLog));
        }

        private static DragonWhisperModel.DropLogEntry ReadDropLog(NetReader reader)
        {
            uint time = reader.ReadU32();
            uint serverId = reader.ReadU32();
            uint serverNum = reader.ReadU32();
            long roleId = reader.ReadU64();
            string name = reader.ReadString();
            uint bossId = reader.ReadU32();
            uint goodsId = reader.ReadU32();
            uint num = reader.ReadU32();
            uint rating = reader.ReadU32();
            var extraAttrs = reader.ReadArray(r => new DragonWhisperModel.EquipExtraAttr(r.ReadU8(), r.ReadU8(), r.ReadU16(), r.ReadU32(), r.ReadU8(), r.ReadU32()));
            byte isTop = reader.ReadU8();
            return new DragonWhisperModel.DropLogEntry(time, serverId, serverNum, roleId, name, bossId, goodsId, num, rating, extraAttrs, isTop);
        }

        public override void Dispose()
        {
            DragonWhisperModel.Instance.Reset();
            base.Dispose();
        }
    }
}
