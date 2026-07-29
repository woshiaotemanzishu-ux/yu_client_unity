using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Unreal
{
    /// <summary>
    /// 幻饰 149xx 读侧控制器。GAME_START 严格请求六个部位的14904，再请求14908；
    /// 14906/14907仅允许业务显式预览，14900只接收原始错误。
    /// </summary>
    public sealed class UnrealController : BaseController
    {
        public static readonly UnrealController Instance = new UnrealController();
        public const byte EquipCellCount = 6;
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private UnrealController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.UNREAL_ERROR, On14900);
            RegisterProtocal(Proto.UNREAL_STRENGTH_INFO, On14904);
            RegisterProtocal(Proto.UNREAL_STAGE_PREVIEW, On14906);
            RegisterProtocal(Proto.UNREAL_DECOMPOSE_PREVIEW, On14907);
            RegisterProtocal(Proto.UNREAL_UNLOCKED_CELLS, On14908);
        }

        public void RequestStartup()
        {
            UnrealModel.Instance.Reset();
            for (byte cell = 1; cell <= EquipCellCount; cell++) RequestStrength(cell);
            RequestUnlockedCells();
        }

        public void RequestStrength(byte cell) => SendRequest(Proto.UNREAL_STRENGTH_INFO, "c", cell);
        public void RequestStagePreview(ulong goodsId) =>
            SendRequest(Proto.UNREAL_STAGE_PREVIEW, "l", unchecked((long)goodsId));
        public void RequestDecomposePreview(ulong goodsId) =>
            SendRequest(Proto.UNREAL_DECOMPOSE_PREVIEW, "l", unchecked((long)goodsId));
        public void RequestUnlockedCells() => SendRequest(Proto.UNREAL_UNLOCKED_CELLS);

        private void SendRequest(int protoId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId, format, args);
        }

        private void On14900(NetReader r)
        {
            UnrealModel.Instance.ReplaceError(r.ReadU32(), r.ReadString());
        }

        private void On14904(NetReader r)
        {
            UnrealModel.Instance.ReplaceStrength(r.ReadU32(), r.ReadU8(), r.ReadU16(), r.ReadU32());
        }

        private void On14906(NetReader r)
        {
            ReadPreview(r, true);
        }

        private void On14907(NetReader r)
        {
            ReadPreview(r, false);
        }

        private static void ReadPreview(NetReader r, bool stage)
        {
            ulong goodsId = unchecked((ulong)r.ReadU64());
            uint overallRating = r.ReadU32();
            List<UnrealModel.PreviewAttr> attrs = r.ReadArray(rr => new UnrealModel.PreviewAttr(
                rr.ReadU8(), rr.ReadU8(), rr.ReadU16(), rr.ReadU32(), rr.ReadU8(), rr.ReadU32()));
            if (stage) UnrealModel.Instance.ReplaceStagePreview(goodsId, overallRating, attrs);
            else UnrealModel.Instance.ReplaceDecomposePreview(goodsId, overallRating, attrs);
        }

        private void On14908(NetReader r)
        {
            UnrealModel.Instance.ReplaceUnlockedCells(r.ReadArray(rr => rr.ReadU8()));
        }

        public override void Dispose()
        {
            UnrealModel.Instance.Reset();
            base.Dispose();
        }
    }
}
