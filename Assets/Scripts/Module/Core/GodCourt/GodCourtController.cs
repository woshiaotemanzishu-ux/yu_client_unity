using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.GodCourt
{
    /// <summary>
    /// 神庭 233xx 读侧控制器。GAME_START 固定请求 23301→23306；仅精确升到 490 级时补拉一次。
    /// 23310 是按 court_id 保存的独立完整推送，不补丁式改写 23301 的原始有序快照。
    /// </summary>
    public sealed class GodCourtController : BaseController
    {
        public static readonly GodCourtController Instance = new GodCourtController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private int _lastLevel = -1;

        private GodCourtController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GOD_COURT_ERROR, On23300);
            RegisterProtocal(Proto.GOD_COURT_OVERVIEW, On23301);
            RegisterProtocal(Proto.GOD_COURT_HOUSE, On23306);
            RegisterProtocal(Proto.GOD_COURT_UPDATE, On23310);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            GodCourtModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        public void RequestStartup()
        {
            GodCourtModel.Instance.Reset();
            _lastLevel = RoleModel.Instance.Level;
            RequestOverview();
            RequestHouse();
        }

        public void RequestOverview() => SendEmpty(Proto.GOD_COURT_OVERVIEW);
        public void RequestHouse() => SendEmpty(Proto.GOD_COURT_HOUSE);

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId);
        }

        private void On23300(NetReader r)
        {
            GodCourtModel.Instance.ReplaceError(r.ReadU32(), r.ReadString());
        }

        private void On23301(NetReader r)
        {
            GodCourtModel.Instance.ReplaceOverview(r.ReadArray(ReadCourt));
        }

        private void On23306(NetReader r)
        {
            ushort rewardLevel = r.ReadU16();
            uint sumNum = r.ReadU32();
            byte crystalColor = r.ReadU8();
            uint dailyNum = r.ReadU32();
            ushort houseLevel = r.ReadU16();
            ushort houseExp = r.ReadU16();
            List<GodCourtModel.GrandStatusEntry> statuses = r.ReadArray(
                rr => new GodCourtModel.GrandStatusEntry(rr.ReadU16(), rr.ReadU8()));
            GodCourtModel.Instance.ReplaceHouse(
                rewardLevel, sumNum, crystalColor, dailyNum, houseLevel, houseExp, statuses);
        }

        private void On23310(NetReader r)
        {
            GodCourtModel.Instance.ReplaceCourtUpdate(ReadCourt(r));
        }

        private static GodCourtModel.CourtEntry ReadCourt(NetReader r)
        {
            uint courtId = r.ReadU32();
            ushort courtLevel = r.ReadU16();
            ulong power = unchecked((ulong)r.ReadU64());
            List<GodCourtModel.AttrEntry> attrs = r.ReadArray(
                rr => new GodCourtModel.AttrEntry(rr.ReadU16(), rr.ReadU32()));
            byte isActive = r.ReadU8();
            List<GodCourtModel.EquipEntry> equips = r.ReadArray(
                rr => new GodCourtModel.EquipEntry(rr.ReadU8(), unchecked((ulong)rr.ReadU64()), rr.ReadU8()));
            List<GodCourtModel.SuitEntry> suits = r.ReadArray(
                rr => new GodCourtModel.SuitEntry(rr.ReadU8(), rr.ReadU16()));
            return new GodCourtModel.CourtEntry(courtId, courtLevel, power, attrs, isActive, equips, suits);
        }

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            if (role.Level != 490) return;
            RequestOverview();
            RequestHouse();
        }
    }
}
