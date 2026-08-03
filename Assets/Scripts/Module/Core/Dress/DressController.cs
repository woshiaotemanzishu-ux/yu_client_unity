using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Dress
{
    /// <summary>装扮快照与设置页显式事务。所有写入只在成功回包后落模型。</summary>
    public sealed class DressController : BaseController
    {
        public static readonly DressController Instance = new DressController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private int _pendingCommand;
        private byte _pendingType;
        private uint _pendingDressId;

        private DressController() { }

        public event Action TransactionStateChanged;
        public bool IsTransactionPending => _pendingCommand != 0;

        protected override void Register()
        {
            RegisterProtocal(Proto.DRESS_INFO, On11200);
            RegisterProtocal(Proto.DRESS_ACTIVATE, On11201);
            RegisterProtocal(Proto.DRESS_USE, On11202);
            RegisterProtocal(Proto.DRESS_TAKE_OFF, On11203);
            RegisterProtocal(Proto.DRESS_INACTIVE_POWER, On11205);
        }

        public void RequestStartup()
        {
            RequestInfo(1);
            RequestInfo(2);
            RequestInfo(3);
            RequestInfo(5);
        }

        public void RequestInfo(byte type) => SendRequest(Proto.DRESS_INFO, "c", type);

        public void RequestInactivePower(byte type, uint dressId)
        {
            if (dressId == 0) return;
            SendRequest(Proto.DRESS_INACTIVE_POWER, "ci", type, dressId);
        }

        public bool ActivateOrUpgrade(byte type, uint dressId)
            => BeginTransaction(Proto.DRESS_ACTIVATE, type, dressId);

        public bool Use(byte type, uint dressId)
            => BeginTransaction(Proto.DRESS_USE, type, dressId);

        public bool TakeOff(byte type, uint dressId)
            => BeginTransaction(Proto.DRESS_TAKE_OFF, type, dressId);

        private bool BeginTransaction(int command, byte type, uint dressId)
        {
            if (dressId == 0 || _pendingCommand != 0) return false;
            _pendingCommand = command;
            _pendingType = type;
            _pendingDressId = dressId;
            TransactionStateChanged?.Invoke();
            SendRequest(command, "ci", type, dressId);
            return true;
        }

        private void CompleteTransaction(int command, byte type, uint dressId)
        {
            if (_pendingCommand != command || _pendingType != type || _pendingDressId != dressId) return;
            _pendingCommand = 0;
            _pendingType = 0;
            _pendingDressId = 0;
            TransactionStateChanged?.Invoke();
        }

        private void SendRequest(int command, string format, params object[] args)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(command, format, args);
                if (s_outboundIntercept(frame)) return;
            }
#endif
            SendFmt(command, format, args);
        }

        private void On11200(NetReader r)
        {
            byte type = r.ReadU8();
            uint used = r.ReadU32();
            int count = r.ReadU16();
            var entries = new List<DressModel.Entry>(count);
            for (int i = 0; i < count; i++)
                entries.Add(new DressModel.Entry(r.ReadU32(), r.ReadU16(), unchecked((ulong)r.ReadU64()), unchecked((ulong)r.ReadU64())));
            DressModel.Instance.Replace(type, used, entries);
        }

        private void On11201(NetReader r)
        {
            int result = r.ReadI32();
            byte type = r.ReadU8();
            uint id = r.ReadU32();
            ushort level = r.ReadU16();
            ulong currentPower = unchecked((ulong)r.ReadU64());
            ulong nextPower = unchecked((ulong)r.ReadU64());
            CompleteTransaction(Proto.DRESS_ACTIVATE, type, id);
            if (result != 1)
            {
                TipsManager.Toast("装扮操作失败（错误码 " + result + "）");
                return;
            }
            DressModel.Instance.ApplyActivation(type, id, level, currentPower, nextPower);
            TipsManager.Toast(level == 1 ? "激活成功" : "升级成功");
        }

        private void On11202(NetReader r)
        {
            int result = r.ReadI32();
            byte type = r.ReadU8();
            uint id = r.ReadU32();
            CompleteTransaction(Proto.DRESS_USE, type, id);
            if (result != 1)
            {
                TipsManager.Toast("使用失败（错误码 " + result + "）");
                return;
            }
            DressModel.Instance.ApplyUsed(type, id);
            TipsManager.Toast("使用成功");
        }

        private void On11203(NetReader r)
        {
            int result = r.ReadI32();
            byte type = r.ReadU8();
            uint id = r.ReadU32();
            CompleteTransaction(Proto.DRESS_TAKE_OFF, type, id);
            if (result != 1)
            {
                TipsManager.Toast("卸下失败（错误码 " + result + "）");
                return;
            }
            DressModel.Instance.ApplyUsed(type, 0);
            TipsManager.Toast("卸下成功");
        }

        private void On11205(NetReader r)
            => DressModel.Instance.ReplaceInactivePower(r.ReadU8(), r.ReadU32(), unchecked((ulong)r.ReadU64()));

        public override void Dispose()
        {
            _pendingCommand = 0;
            _pendingType = 0;
            _pendingDressId = 0;
            DressModel.Instance.Reset();
            base.Dispose();
        }
    }
}
