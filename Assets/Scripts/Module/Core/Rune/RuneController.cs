using System;
using System.Collections.Generic;
using System.Linq;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>九霄劫魄协议控制器。15010/15017/15018 由 BagController 单点注册并转存 RuneModel。</summary>
    public sealed class RuneController : BaseController
    {
        public static readonly RuneController Instance = new RuneController();
        private static Func<byte[], bool> s_outboundIntercept;

        public const int RUNE_BAG_POS = 11;
        private const int RuneExchange = 16703;

        private RuneController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.RUNE_INFO, On16700);
            RegisterProtocal(Proto.RUNE_WEAR, On16701);
            RegisterProtocal(Proto.RUNE_UPGRADE, On16702);
            RegisterProtocal(RuneExchange, On16703);
            RegisterProtocal(Proto.RUNE_DUNGEON_LEVEL, On16704);
            RegisterProtocal(Proto.RUNE_COMPOSE_PREVIEW, On16705);
            RegisterProtocal(Proto.RUNE_DECOMPOSE_PREVIEW, On16706);
            RegisterProtocal(Proto.RUNE_DISMANTLE_PREVIEW, On16709);
        }

        public override void Dispose()
        {
            RuneModel.Instance.Clear();
            base.Dispose();
        }

        public void RequestInfo()
        {
            SendRequest(Proto.RUNE_INFO);
            GameLog.Info("Rune", "request 16700 rune info");
        }

        public void RequestStartup()
        {
            RequestInfo();
            RequestDungeonLevel();
        }

        public void RequestDungeonLevel() => SendRequest(Proto.RUNE_DUNGEON_LEVEL);

        public void RequestComposePreview(ulong ruleId, IReadOnlyList<ulong> goodsIds)
        {
            int count = CheckedCount(goodsIds);
            var args = new object[count + 2];
            args[0] = unchecked((long)ruleId);
            args[1] = count;
            for (int i = 0; i < count; i++) args[i + 2] = unchecked((long)goodsIds[i]);
            SendRequest(Proto.RUNE_COMPOSE_PREVIEW, "lh" + new string('l', count), args);
        }

        public void RequestDecomposePreview(IReadOnlyList<ulong> goodsIds) =>
            RequestGoodsListPreview(Proto.RUNE_DECOMPOSE_PREVIEW, goodsIds);

        public void RequestDismantlePreview(IReadOnlyList<ulong> goodsIds) =>
            RequestGoodsListPreview(Proto.RUNE_DISMANTLE_PREVIEW, goodsIds);

        public void RequestRuneBag()
        {
            SendFmt(Proto.GOODS_CONTAINER_INFO, "h", RUNE_BAG_POS);
            GameLog.Info("Rune", "request 15010 rune_bag pos={0}", RUNE_BAG_POS);
        }

        public void Wear(int posId, long goodsId)
        {
            if (posId <= 0 || goodsId <= 0) return;
            SendFmt(Proto.RUNE_WEAR, "cl", posId, goodsId);
            GameLog.Info("Rune", "wear 16701 pos_id={0} goods_id={1}", posId, goodsId);
        }

        public void Upgrade(long goodsId)
        {
            if (goodsId <= 0) return;
            SendFmt(Proto.RUNE_UPGRADE, "l", goodsId);
            GameLog.Info("Rune", "upgrade 16702 goods_id={0}", goodsId);
        }

        public void Exchange(int exchangeId, int count = 1)
        {
            if (exchangeId <= 0 || count <= 0) return;
            SendRequest(RuneExchange, "hi", exchangeId, count);
            GameLog.Info("Rune", "exchange 16703 id={0} count={1}", exchangeId, count);
        }

        private void On16700(NetReader reader)
        {
            int runePoint = (int)reader.ReadU32();
            int runeChip = (int)reader.ReadU32();
            int skillLv = reader.ReadU16();
            List<RuneModel.SlotVo> slots = reader.ReadArray(ReadSlot);
            long sumPower = reader.ReadI64();

            RuneModel.Instance.Apply16700(runePoint, runeChip, skillLv, slots, sumPower);
            GameLog.Info("Rune", "16700 point={0} chip={1} skillLv={2} slots={3} sumPower={4} remaining={5}B",
                runePoint, runeChip, skillLv, slots.Count, sumPower, reader.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_RUNE_UPDATE);
        }

        private void On16701(NetReader reader)
        {
            int code = (int)reader.ReadU32();
            int posId = reader.ReadU8();
            long newGoodsId = reader.ReadU64();
            reader.ReadU64();
            int newGoodsTypeId = (int)reader.ReadU32();

            if (code != 1)
            {
                TipsManager.Toast("镶嵌失败(" + code + ")");
                return;
            }

            RuneModel.Instance.Apply16701(posId, newGoodsId, newGoodsTypeId);
            TipsManager.Toast("镶嵌成功");
            EventDispatcher.Emit(GlobalEvent.EVT_RUNE_UPDATE);
            RequestInfo();
        }

        private void On16702(NetReader reader)
        {
            int code = (int)reader.ReadU32();
            int runePoint = (int)reader.ReadU32();
            long goodsId = reader.ReadU64();

            if (code != 1)
            {
                TipsManager.Toast("强化失败(" + code + ")");
                GameLog.Info("Rune", "16702 upgrade fail code={0} goods_id={1}", code, goodsId);
                return;
            }

            RuneModel.Instance.ApplyUpgradeSuccess(goodsId, runePoint);
            TipsManager.Toast("强化成功");
            EventDispatcher.Emit(GlobalEvent.EVT_RUNE_UPDATE);
            RequestInfo();
        }

        private void On16703(NetReader reader)
        {
            int code = (int)reader.ReadU32();
            int runeChip = (int)reader.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("兑换失败(" + code + ")");
                return;
            }
            RuneModel.Instance.ApplyExchangeSuccess(runeChip);
            TipsManager.Toast("兑换成功");
        }

        private void On16704(NetReader reader) =>
            RuneModel.Instance.ReplaceDungeonLevel(reader.ReadU16());

        private void On16705(NetReader reader) =>
            RuneModel.Instance.ReplaceComposePreview(reader.ReadU32(), reader.ReadU32());

        private void On16706(NetReader reader) =>
            RuneModel.Instance.ReplaceDecomposePreview(
                reader.ReadU32(), unchecked((ulong)reader.ReadU64()), reader.ReadArray(ReadObjectEntry));

        private void On16709(NetReader reader) =>
            RuneModel.Instance.ReplaceDismantlePreview(reader.ReadU32(), reader.ReadArray(ReadObjectEntry));

        private static RuneModel.SlotVo ReadSlot(NetReader reader)
        {
            var value = new RuneModel.SlotVo
            {
                PosId = reader.ReadU8(),
                IfOpen = reader.ReadU8() != 0,
                GoodsId = reader.ReadU64(),
                GoodsTypeId = (int)reader.ReadU32(),
                Color = reader.ReadU8(),
                Lv = reader.ReadU16(),
            };
            value.Attrs = Array.AsReadOnly(reader.ReadArray(ReadAttr).ToArray());
            return value;
        }

        private static RuneModel.RuneAttrVo ReadAttr(NetReader reader) =>
            new RuneModel.RuneAttrVo(
                (int)reader.ReadU32(),
                reader.ReadU32(),
                (int)reader.ReadU32(),
                reader.ReadU32(),
                reader.ReadU64(),
                reader.ReadU64());

        private static RuneModel.ObjectEntry ReadObjectEntry(NetReader reader) =>
            new RuneModel.ObjectEntry(reader.ReadU8(), reader.ReadU32(), reader.ReadU32());

        private void RequestGoodsListPreview(int protocolId, IReadOnlyList<ulong> goodsIds)
        {
            int count = CheckedCount(goodsIds);
            var args = new object[count + 1];
            args[0] = count;
            for (int i = 0; i < count; i++) args[i + 1] = unchecked((long)goodsIds[i]);
            SendRequest(protocolId, "h" + new string('l', count), args);
        }

        private static int CheckedCount(IReadOnlyList<ulong> goodsIds)
        {
            int count = goodsIds?.Count ?? 0;
            if (count > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(goodsIds));
            return count;
        }

        private void SendRequest(int protocolId, string format = null, params object[] args)
        {
            if (s_outboundIntercept == null)
            {
                SendFmt(protocolId, format, args);
                return;
            }
            byte[] frame = UserMsgAdapter.Encode(protocolId, format, args);
            if (s_outboundIntercept(frame)) return;
            SendFmt(protocolId, format, args);
        }
    }
}
