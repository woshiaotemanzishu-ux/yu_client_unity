using System;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Eternity
{
    /// <summary>
    /// 永恒圣殿原始数据底座：时间、参与资格、伤害排行、复活状态与 Boss 状态推送。
    /// 老端仅在 GAME_START 时等级达到门槛请求时间快照，并且只在等级精确升至 480 时补发；
    /// 27908 仅接收服务端场景广播，不增加请求、等级或启动链。
    /// </summary>
    public sealed class EternityController : BaseController
    {
        public const int OPEN_LEVEL = 480;

        public static readonly EternityController Instance = new EternityController();

        private int _lastLevel = -1;

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private EternityController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.ETERNITY_TIME_INFO, On27900);
            RegisterProtocal(Proto.ETERNITY_JOIN_INFO, On27901);
            RegisterProtocal(Proto.ETERNITY_DAMAGE_RANK, On27905);
            RegisterProtocal(Proto.ETERNITY_RELIVE_INFO, On27906);
            RegisterProtocal(Proto.ETERNITY_BOSS_STATE, On27908);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public void RequestStartup()
        {
            EternityModel.Instance.Reset();
            _lastLevel = RoleModel.Instance.Level;
            if (_lastLevel >= OPEN_LEVEL)
            {
                RequestInfo();
            }
        }

        private void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.ETERNITY_TIME_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.ETERNITY_TIME_INFO);
        }

        public void RequestJoinInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.ETERNITY_JOIN_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.ETERNITY_JOIN_INFO);
        }

        public void RequestReliveInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.ETERNITY_RELIVE_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.ETERNITY_RELIVE_INFO);
        }

        public void RequestDamageRank(ushort scene, uint monId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.ETERNITY_DAMAGE_RANK, "hi", new object[] { scene, monId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.ETERNITY_DAMAGE_RANK, "hi", scene, monId);
        }

        private void On27900(NetReader reader)
        {
            uint openTime = reader.ReadU32();
            uint enterTime = reader.ReadU32();
            uint endTime = reader.ReadU32();
            EternityModel.Instance.Replace(openTime, enterTime, endTime);
        }

        private void On27901(NetReader reader)
        {
            byte canEnterScene = reader.ReadU8();
            var joins = reader.ReadArray(r => new EternityModel.JoinEntry(r.ReadU32(), r.ReadU16(), r.ReadU16()));
            EternityModel.Instance.ReplaceJoinInfo(canEnterScene, joins);
        }

        private void On27905(NetReader reader)
        {
            ushort scene = reader.ReadU16();
            uint monId = reader.ReadU32();
            var entries = reader.ReadArray(r => new EternityModel.DamageEntry(r.ReadU32(), r.ReadU16(), r.ReadString(), r.ReadU32(), r.ReadString(), r.ReadU16()));
            EternityModel.Instance.ReplaceDamageRank(scene, monId, entries);
        }

        private void On27906(NetReader reader)
        {
            EternityModel.Instance.ReplaceReliveInfo(reader.ReadU16(), reader.ReadU32(), reader.ReadU32(), reader.ReadU32());
        }

        private void On27908(NetReader reader)
        {
            EternityModel.Instance.ReplaceBossState(new EternityModel.BossStateEntry(reader.ReadU32(), reader.ReadU32(), reader.ReadU32(), reader.ReadU32(), reader.ReadString()));
        }

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Level == _lastLevel)
            {
                return;
            }

            _lastLevel = role.Level;
            if (_lastLevel == OPEN_LEVEL)
            {
                RequestInfo();
            }
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            _lastLevel = -1;
            EternityModel.Instance.Reset();
            base.Dispose();
        }
    }
}
