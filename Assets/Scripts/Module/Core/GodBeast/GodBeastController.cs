using System;
using System.Collections.Generic;
using System.Text;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.GodBeast
{
    /// <summary>幻兽读侧控制器；GAME_START 只查询 17301，其余查询均由业务显式触发。</summary>
    public sealed class GodBeastController : BaseController
    {
        public static readonly GodBeastController Instance = new GodBeastController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private GodBeastController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GODBEAST_ERROR, On17300);
            RegisterProtocal(Proto.GODBEAST_OVERVIEW, On17301);
            RegisterProtocal(Proto.GODBEAST_UPDATE, On17302);
            RegisterProtocal(Proto.GODBEAST_STRENGTH_PREVIEW, On17308);
            RegisterProtocal(Proto.GODBEAST_ATTRIBUTE_POWER, On17309);
        }

        public void RequestStartup() => SendRequest(Proto.GODBEAST_OVERVIEW);

        public void RequestStrengthPreview(ulong goodsId, byte isDouble, IReadOnlyList<ulong> materialGoodsIds)
        {
            int count = materialGoodsIds?.Count ?? 0;
            if (count > ushort.MaxValue) return;

            var args = new object[count + 3];
            args[0] = unchecked((long)goodsId);
            args[1] = isDouble;
            args[2] = count;
            for (int i = 0; i < count; i++) args[i + 3] = unchecked((long)materialGoodsIds[i]);
            SendRequest(Proto.GODBEAST_STRENGTH_PREVIEW, "lch" + new string('l', count), args);
        }

        public void RequestAttributePower(ushort moduleId, byte subModuleId, IReadOnlyList<GodBeastModel.Attr> attrs)
        {
            int count = attrs?.Count ?? 0;
            if (count > ushort.MaxValue) return;
            for (int i = 0; i < count; i++)
                if (attrs[i] == null) return;

            var args = new object[count * 2 + 3];
            args[0] = moduleId;
            args[1] = subModuleId;
            args[2] = count;
            var format = new StringBuilder("hch", 3 + count * 2);
            for (int i = 0; i < count; i++)
            {
                format.Append("hi");
                args[i * 2 + 3] = attrs[i].Type;
                args[i * 2 + 4] = attrs[i].Value;
            }
            SendRequest(Proto.GODBEAST_ATTRIBUTE_POWER, format.ToString(), args);
        }

        private void SendRequest(int protoId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId, format, args);
        }

        private void On17300(NetReader r)
        {
            GodBeastModel.Instance.SetError(r.ReadU32(), r.ReadString());
        }

        private void On17301(NetReader r)
        {
            byte fightCount = r.ReadU8();
            int count = r.ReadU16();
            var beasts = new List<GodBeastModel.Beast>(count);
            for (int i = 0; i < count; i++) beasts.Add(ReadBeast(r));
            GodBeastModel.Instance.ReplaceData(fightCount, beasts);
        }

        private void On17302(NetReader r)
        {
            GodBeastModel.Instance.ApplyBeastUpdate(ReadBeast(r));
        }

        private void On17308(NetReader r)
        {
            GodBeastModel.Instance.ReplaceStrengthPreview(unchecked((ulong)r.ReadU64()), r.ReadU16(), r.ReadU32());
        }

        private void On17309(NetReader r)
        {
            ushort moduleId = r.ReadU16();
            byte subModuleId = r.ReadU8();
            GodBeastModel.Instance.ReplaceAttributePower(moduleId, subModuleId, r.ReadU32());
        }

        private static GodBeastModel.Beast ReadBeast(NetReader r)
        {
            uint id = r.ReadU32();
            byte state = r.ReadU8();
            uint score = r.ReadU32();
            int equipCount = r.ReadU16();
            var equips = new List<GodBeastModel.Equip>(equipCount);
            for (int i = 0; i < equipCount; i++)
                equips.Add(new GodBeastModel.Equip(r.ReadU8(), unchecked((ulong)r.ReadU64()), r.ReadU16(), r.ReadU32()));

            int attrCount = r.ReadU16();
            var attrs = new List<GodBeastModel.Attr>(attrCount);
            for (int i = 0; i < attrCount; i++)
                attrs.Add(new GodBeastModel.Attr(r.ReadU16(), r.ReadU32()));
            return new GodBeastModel.Beast(id, state, score, equips, attrs);
        }

        public override void Dispose()
        {
            GodBeastModel.Instance.Reset();
            base.Dispose();
        }
    }
}
