using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Bag;

namespace Shenxiao.Module.Core.Designation
{
    /// <summary>
    /// 41101 权威列表、41104/41105/41107/41108 独立只读切片与 41109 道具激活事务。
    /// 41109 只从真实称号页进入，发送前核对权威列表、配置和背包；成功后重查 41101，不做本地扣物或乐观激活。
    /// </summary>
    public sealed class DesignationController : BaseController
    {
        public static readonly DesignationController Instance = new DesignationController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private const long ActivationTimeoutTicks = TimeSpan.TicksPerSecond * 10L;
        private bool _activationPending;
        private long _activationStartedTicks;
        private uint _activationRefreshPendingId;

        private DesignationController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.DESIGNATION_LIST, On41101);
            RegisterProtocal(Proto.DESIGNATION_ACTIVATED, On41104);
            RegisterProtocal(Proto.DESIGNATION_SCENE_NOTICE, On41105);
            RegisterProtocal(Proto.DESIGNATION_POWER, On41107);
            RegisterProtocal(Proto.DESIGNATION_REMOVED, On41108);
            RegisterProtocal(Proto.DESIGNATION_ACTIVATE_BY_GOODS, On41109);
        }

        public void RequestStartup() => SendEmpty(Proto.DESIGNATION_LIST);

        /// <summary>显式查询一个称号的战力；无回复时保留上一次 41107 快照。</summary>
        public void RequestPower(uint designationId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.DESIGNATION_POWER, "i", new object[] { designationId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.DESIGNATION_POWER, "i", designationId);
        }

        public bool HasPendingActivation
        {
            get
            {
                RefreshActivationTimeout();
                return _activationPending;
            }
        }

        public bool IsAwaitingActivationRefresh(uint designationId)
            => designationId != 0 && _activationRefreshPendingId == designationId;

        /// <summary>
        /// 从称号详情页发起 41109。无权威列表、已激活、配置非单背包物品、背包未加载或材料不足时均拒绝。
        /// </summary>
        public bool TryActivateByGoods(uint designationId)
        {
            RefreshActivationTimeout();
            if (_activationPending)
            {
                TipsManager.Toast("称号激活请求处理中");
                return false;
            }
            if (!DesignationModel.Instance.HasData)
            {
                TipsManager.Toast("称号数据尚未加载");
                return false;
            }
            if (DesignationModel.Instance.GetEntry(designationId) != null)
            {
                TipsManager.Toast("称号已激活");
                return false;
            }
            if (IsAwaitingActivationRefresh(designationId))
            {
                TipsManager.Toast("称号状态刷新中");
                return false;
            }
            if (!DesignationConfigs.TryGetActivationCost(designationId, out DesignationConfigs.Cost cost))
            {
                TipsManager.Toast("该称号不能使用道具激活");
                return false;
            }
            if (!BagModel.Instance.HasData)
            {
                TipsManager.Toast("背包数据尚未加载");
                return false;
            }
            if (BagModel.Instance.GetTypeGoodsNum(cost.TypeId) < cost.Num)
            {
                TipsManager.Toast("激活材料不足");
                return false;
            }

            _activationPending = true;
            _activationStartedTicks = DateTime.UtcNow.Ticks;
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(
                Proto.DESIGNATION_ACTIVATE_BY_GOODS, "i", new object[] { designationId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return true;
#endif
            SendFmt(Proto.DESIGNATION_ACTIVATE_BY_GOODS, "i", designationId);
            return true;
        }

        private void SendEmpty(int command)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(command, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(command);
        }

        private void On41101(NetReader reader)
        {
            uint current = reader.ReadU32();
            int count = reader.ReadU16();
            var entries = new List<DesignationModel.Entry>(count);
            for (int i = 0; i < count; i++)
                entries.Add(new DesignationModel.Entry(reader.ReadU32(), reader.ReadU8(), reader.ReadU32()));
            DesignationModel.Instance.ReplaceData(current, entries);
            _activationRefreshPendingId = 0;
            EventDispatcher.Emit(GlobalEvent.EVT_DESIGNATION_LIST_UPDATE);
        }

        private void On41104(NetReader reader)
            => DesignationModel.Instance.ReplaceActivation(reader.ReadU32(), reader.ReadU32(), reader.ReadU32());

        private void On41105(NetReader reader)
            => DesignationModel.Instance.ReplaceSceneNotice(unchecked((ulong)reader.ReadU64()), reader.ReadU32());

        private void On41107(NetReader reader)
            => DesignationModel.Instance.ReplacePowerQuery(reader.ReadU32(), reader.ReadU32());

        private void On41108(NetReader reader)
            => DesignationModel.Instance.ReplaceRemoval(reader.ReadU32());

        private void On41109(NetReader reader)
        {
            uint code = reader.ReadU32();
            uint power = reader.ReadU32();
            uint currentUsed = reader.ReadU32();
            uint designationId = reader.ReadU32();
            DesignationModel.Instance.ReplaceGoodsActivationResult(code, power, currentUsed, designationId);
            ClearActivationPending();
            if (code == 1) _activationRefreshPendingId = designationId;
            EventDispatcher.Emit(GlobalEvent.EVT_DESIGNATION_ACTIVATION_RESULT);
            if (code == 1)
            {
                TipsManager.Toast("称号激活成功");
                RequestStartup();
            }
            else
            {
                TipsManager.Toast("称号激活失败(" + code + ")");
            }
        }

        private void RefreshActivationTimeout()
        {
            if (!_activationPending) return;
            long elapsed = DateTime.UtcNow.Ticks - _activationStartedTicks;
            if (elapsed >= 0 && elapsed < ActivationTimeoutTicks) return;
            ClearActivationPending();
        }

        private void ClearActivationPending()
        {
            _activationPending = false;
            _activationStartedTicks = 0;
        }

        public override void Dispose()
        {
            ClearActivationPending();
            _activationRefreshPendingId = 0;
            DesignationModel.Instance.Reset();
            base.Dispose();
        }
    }
}
