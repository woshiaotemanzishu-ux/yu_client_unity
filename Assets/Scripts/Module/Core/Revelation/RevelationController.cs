using System;
using System.Collections.Generic;
using System.Text;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Revelation
{
    /// <summary>启示圣铠 286 家族：失败出口、穿脱、聚灵、技能、总览、形象与战力闭环。</summary>
    public sealed class RevelationController : BaseController
    {
        public static readonly RevelationController Instance = new RevelationController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private RevelationController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.REVELATION_ERROR, On28600);
            RegisterProtocal(Proto.REVELATION_EQUIP, On28601);
            RegisterProtocal(Proto.REVELATION_UNLOAD, On28602);
            RegisterProtocal(Proto.REVELATION_DEVOUR, On28603);
            RegisterProtocal(Proto.REVELATION_GATHER_UP, On28604);
            RegisterProtocal(Proto.REVELATION_SKILL_UP, On28605);
            RegisterProtocal(Proto.REVELATION_INFO, On28606);
            RegisterProtocal(Proto.REVELATION_FIGURE, On28607);
            RegisterProtocal(Proto.REVELATION_POWER, On28609);
        }

        public void RequestStartup() => SendRequest(Proto.REVELATION_INFO);
        public void RequestPower() => SendRequest(Proto.REVELATION_POWER);
        public void Equip(ulong goodsId) => SendRequest(Proto.REVELATION_EQUIP, "l", unchecked((long)goodsId));
        public void Unload(ulong goodsId) => SendRequest(Proto.REVELATION_UNLOAD, "l", unchecked((long)goodsId));
        public void GatherUp(byte position) => SendRequest(Proto.REVELATION_GATHER_UP, "c", position);
        public void SkillUp(uint skillId) => SendRequest(Proto.REVELATION_SKILL_UP, "i", skillId);
        public void UseFigure(ushort figureId) => SendRequest(Proto.REVELATION_FIGURE, "h", figureId);

        public void Devour(byte position, IReadOnlyList<ulong> goodsIds)
        {
            if (goodsIds == null || goodsIds.Count == 0) return;
            var format = new StringBuilder("ch");
            var args = new List<object>(2 + goodsIds.Count) { position, goodsIds.Count };
            for (int i = 0; i < goodsIds.Count; i++)
            {
                format.Append('l');
                args.Add(unchecked((long)goodsIds[i]));
            }
            SendRequest(Proto.REVELATION_DEVOUR, format.ToString(), args.ToArray());
        }

        private void On28600(NetReader r) => RevelationModel.Instance.ApplyError(r.ReadU32());

        private void On28601(NetReader r)
        {
            uint result = r.ReadU32();
            ulong goodsId = unchecked((ulong)r.ReadU64());
            ulong oldGoodsId = unchecked((ulong)r.ReadU64());
            uint typeId = r.ReadU32();
            byte cellPos = r.ReadU8();
            RevelationModel.Instance.ApplyEquip(result, goodsId, oldGoodsId, typeId, cellPos);
            if (result == 1) RequestStartup();
            else RevelationModel.Instance.ApplyError(result);
        }

        private void On28602(NetReader r)
        {
            uint result = r.ReadU32();
            ulong goodsId = unchecked((ulong)r.ReadU64());
            ushort cell = r.ReadU16();
            RevelationModel.Instance.ApplyUnload(result, goodsId, cell);
            if (result == 1) RequestStartup();
            else RevelationModel.Instance.ApplyError(result);
        }

        private void On28603(NetReader r) =>
            RevelationModel.Instance.ApplyGathering(r.ReadU8(), r.ReadU8(), r.ReadU32());

        private void On28604(NetReader r)
        {
            RevelationModel.Instance.ApplyGathering(r.ReadU8(), r.ReadU8(), r.ReadU32());
            RequestStartup();
        }

        private void On28605(NetReader r)
        {
            RevelationModel.Instance.ApplySkill(r.ReadU32(), r.ReadU16());
            RequestStartup();
        }

        private void On28606(NetReader r)
        {
            ushort max = r.ReadU16();
            ushort current = r.ReadU16();
            ulong power = unchecked((ulong)r.ReadU64());
            int gatheringCount = r.ReadU16();
            var gatherings = new List<RevelationModel.Gathering>(gatheringCount);
            for (int i = 0; i < gatheringCount; i++)
                gatherings.Add(new RevelationModel.Gathering(r.ReadU8(), r.ReadU16(), r.ReadU32(), r.ReadU8()));
            int suitCount = r.ReadU16();
            var suits = new List<RevelationModel.Suit>(suitCount);
            for (int i = 0; i < suitCount; i++) suits.Add(new RevelationModel.Suit(r.ReadU32(), r.ReadU32()));
            int skillCount = r.ReadU16();
            var skills = new List<RevelationModel.Skill>(skillCount);
            for (int i = 0; i < skillCount; i++) skills.Add(new RevelationModel.Skill(r.ReadU32(), r.ReadU16()));
            RevelationModel.Instance.Replace(max, current, power, gatherings, suits, skills);
        }

        private void On28607(NetReader r) => RevelationModel.Instance.ApplyFigure(r.ReadU16(), r.ReadU16());
        private void On28609(NetReader r) => RevelationModel.Instance.ReplacePowerIfLoaded(unchecked((ulong)r.ReadU64()));

        private void SendRequest(int protocol, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocol, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            if (string.IsNullOrEmpty(format)) SendFmt(protocol);
            else SendFmt(protocol, format, args);
        }

        public override void Dispose()
        {
            RevelationModel.Instance.Reset();
            base.Dispose();
        }
    }
}
