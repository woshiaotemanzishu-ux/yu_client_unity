using System;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Medal
{
    /// <summary>勋章原始协议切片：13400 仅保存错误码，13401/13405 保存快照；不在数据层接 UI 或形成协议回环。</summary>
    public sealed class MedalController : BaseController
    {
        public static readonly MedalController Instance = new MedalController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private MedalController() { }
        protected override void Register()
        {
            RegisterProtocal(Proto.MEDAL_ERROR, On13400);
            RegisterProtocal(Proto.MEDAL_INFO, On13401);
            RegisterProtocal(Proto.MEDAL_UPGRADE, On13402);
            RegisterProtocal(Proto.MEDAL_TITLE_UPGRADE, On13403);
            RegisterProtocal(Proto.MEDAL_TITLE_EQUIP, On13404);
            RegisterProtocal(Proto.MEDAL_TITLE_SNAPSHOT, On13405);
            RegisterProtocal(Proto.MEDAL_TITLE_UNEQUIP, On13406);
        }
        public void RequestInfo()
        {
            SendEmpty(Proto.MEDAL_INFO);
        }
        public void RequestStartup()
        {
            RequestInfo();
            SendEmpty(Proto.MEDAL_TITLE_SNAPSHOT);
        }
        /// <summary>严格空包。只允许 MedalFlow 在真实点击且全部前置条件满足后调用。</summary>
        public void RequestUpgrade()
        {
            SendEmpty(Proto.MEDAL_UPGRADE);
        }
        public void RequestTitleUpgrade(uint id)
        {
            if (id == 0) return;
            Send(Proto.MEDAL_TITLE_UPGRADE, "i", id);
        }
        public void RequestTitleEquip(uint id)
        {
            if (id == 0) return;
            Send(Proto.MEDAL_TITLE_EQUIP, "i", id);
        }
        public void RequestTitleUnequip()
        {
            SendEmpty(Proto.MEDAL_TITLE_UNEQUIP);
        }
        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId);
        }
        private void Send(int protoId, string format, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId, format, args);
        }
        private void On13400(NetReader r)
        {
            MedalModel.Instance.SetError(r.ReadU32());
        }
        private void On13401(NetReader r)
        {
            MedalModel.Instance.ReplaceData(r.ReadU32(), r.ReadU32(), r.ReadU32(), unchecked((ulong)r.ReadU64()), r.ReadU32(), r.ReadU32());
        }
        private void On13402(NetReader r)
        {
            MedalModel.Instance.ApplyUpgrade(r.ReadU32(), unchecked((ulong)r.ReadU64()));
        }
        private void On13403(NetReader r)
        {
            uint code = r.ReadU32();
            uint id = r.ReadU32();
            ushort level = r.ReadU16();
            r.ReadU32(); // power；完整称号状态由紧随其后的 13405 权威刷新
            r.ReadU8();  // is_equip
            MedalModel.Instance.NotifyTitleOperation(
                MedalModel.TitleOperationKind.Upgrade, id, level, code);
            if (code == 1) SendEmpty(Proto.MEDAL_TITLE_SNAPSHOT);
        }
        private void On13404(NetReader r)
        {
            uint id = r.ReadU32();
            uint code = r.ReadU32();
            MedalModel.Instance.NotifyTitleOperation(
                MedalModel.TitleOperationKind.Equip, id, 0, code);
            if (code == 1) SendEmpty(Proto.MEDAL_TITLE_SNAPSHOT);
        }
        private void On13405(NetReader r)
        {
            int count = r.ReadU16(); var titles = new System.Collections.Generic.List<MedalModel.TitleEntry>(count);
            for (int i = 0; i < count; i++) titles.Add(new MedalModel.TitleEntry(r.ReadU32(), r.ReadU16(), r.ReadU32(), r.ReadU8()));
            MedalModel.Instance.ReplaceTitles(titles);
        }
        private void On13406(NetReader r)
        {
            uint code = r.ReadU32();
            MedalModel.Instance.NotifyTitleOperation(
                MedalModel.TitleOperationKind.Unequip, 0, 0, code);
            if (code == 1) SendEmpty(Proto.MEDAL_TITLE_SNAPSHOT);
        }
        public override void Dispose()
        {
            MedalModel.Instance.Reset();
            base.Dispose();
        }
    }
}
