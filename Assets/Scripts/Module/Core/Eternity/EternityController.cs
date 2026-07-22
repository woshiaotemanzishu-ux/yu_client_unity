using System;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Eternity
{
    /// <summary>
    /// 永恒圣殿的时间快照。老端仅在 GAME_START 时等级达到门槛请求，
    /// 并且只在等级精确升至 480 时补发；跳过 480 不补发。
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

        private void On27900(NetReader reader)
        {
            uint openTime = reader.ReadU32();
            uint enterTime = reader.ReadU32();
            uint endTime = reader.ReadU32();
            EternityModel.Instance.Replace(openTime, enterTime, endTime);
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
