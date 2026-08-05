using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;

namespace Shenxiao.Module.Core.Designation
{
    /// <summary>
    /// 41101 权威列表、41104/41105/41107/41108 读链与 41102/41103/41106/41109 受控写事务。
    /// 写事务只从真实称号页进入，发送前核对权威列表、配置、时效和背包；成功后重查 41101，
    /// 不做本地扣物、阶级/佩戴补丁或乐观激活。
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
        private bool _upgradePending;
        private long _upgradeStartedTicks;
        private uint _upgradeRefreshPendingId;
        private bool _wearPending;
        private long _wearStartedTicks;
        private uint _wearRequestedId;
        private uint _wearRefreshPendingId;

        private DesignationController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.DESIGNATION_LIST, On41101);
            RegisterProtocal(Proto.DESIGNATION_WEAR, On41102);
            RegisterProtocal(Proto.DESIGNATION_UNWEAR, On41103);
            RegisterProtocal(Proto.DESIGNATION_ACTIVATED, On41104);
            RegisterProtocal(Proto.DESIGNATION_SCENE_NOTICE, On41105);
            RegisterProtocal(Proto.DESIGNATION_UPGRADE, On41106);
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
                RefreshWriteTimeouts();
                return _activationPending;
            }
        }

        public bool HasPendingUpgrade
        {
            get
            {
                RefreshWriteTimeouts();
                return _upgradePending;
            }
        }

        public bool HasPendingWear
        {
            get
            {
                RefreshWriteTimeouts();
                return _wearPending;
            }
        }

        public bool IsAwaitingActivationRefresh(uint designationId)
            => designationId != 0 && _activationRefreshPendingId == designationId;

        public bool IsAwaitingUpgradeRefresh(uint designationId)
            => designationId != 0 && _upgradeRefreshPendingId == designationId;

        public bool IsAwaitingWearRefresh(uint designationId)
            => designationId != 0 && _wearRefreshPendingId == designationId;

        private bool HasAnyPendingOrRefresh()
        {
            RefreshWriteTimeouts();
            return _activationPending || _upgradePending || _wearPending
                || _activationRefreshPendingId != 0 || _upgradeRefreshPendingId != 0
                || _wearRefreshPendingId != 0;
        }

        /// <summary>
        /// 真实详情按钮的佩戴/卸下入口。必须持有 41101 权威实例、配置存在且未过期；
        /// 41102/41103 与激活、升阶共用单飞，成功只重查 41101，不乐观改 CurrentUsedId。
        /// </summary>
        public bool TryToggleWear(uint designationId)
        {
            if (HasAnyPendingOrRefresh())
            {
                TipsManager.Toast("称号操作处理中");
                return false;
            }
            if (!DesignationModel.Instance.HasData)
            {
                TipsManager.Toast("称号数据尚未加载");
                return false;
            }
            DesignationModel.Entry entry = DesignationModel.Instance.GetEntry(designationId);
            if (entry == null)
            {
                TipsManager.Toast("称号尚未激活");
                return false;
            }
            if (DesignationConfigs.Get(designationId) == null)
            {
                TipsManager.Toast("称号配置缺失");
                return false;
            }
            if (entry.EndTime != 0 && entry.EndTime <= TimeUtil.NowSec())
            {
                TipsManager.Toast("称号已过期");
                return false;
            }

            bool unwear = DesignationModel.Instance.CurrentUsedId == designationId;
            int command = unwear ? Proto.DESIGNATION_UNWEAR : Proto.DESIGNATION_WEAR;
            _wearPending = true;
            _wearStartedTicks = DateTime.UtcNow.Ticks;
            _wearRequestedId = designationId;
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(command, "i", new object[] { designationId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return true;
#endif
            SendFmt(command, "i", designationId);
            return true;
        }

        /// <summary>
        /// 从称号详情页发起 41109。无权威列表、已激活、配置非单背包物品、背包未加载或材料不足时均拒绝。
        /// </summary>
        public bool TryActivateByGoods(uint designationId)
        {
            RefreshWriteTimeouts();
            if (HasAnyPendingOrRefresh())
            {
                TipsManager.Toast("称号操作处理中");
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

        /// <summary>
        /// 从称号详情页发起 41106。必须已有权威实例、未满阶、当前阶与下一阶配置完整，
        /// 且当前阶 consume 为一条足量的真实背包物品。
        /// </summary>
        public bool TryUpgrade(uint designationId)
        {
            RefreshWriteTimeouts();
            if (HasAnyPendingOrRefresh())
            {
                TipsManager.Toast("称号操作处理中");
                return false;
            }
            if (!DesignationModel.Instance.HasData)
            {
                TipsManager.Toast("称号数据尚未加载");
                return false;
            }
            DesignationModel.Entry entry = DesignationModel.Instance.GetEntry(designationId);
            if (entry == null)
            {
                TipsManager.Toast("称号尚未激活");
                return false;
            }
            if (!DesignationConfigs.TryGetUpgradeCost(designationId, entry.Order, out DesignationConfigs.Cost cost))
            {
                TipsManager.Toast("称号已满阶或升阶配置不完整");
                return false;
            }
            if (!BagModel.Instance.HasData)
            {
                TipsManager.Toast("背包数据尚未加载");
                return false;
            }
            if (BagModel.Instance.GetTypeGoodsNum(cost.TypeId) < cost.Num)
            {
                TipsManager.Toast("升阶材料不足");
                return false;
            }

            _upgradePending = true;
            _upgradeStartedTicks = DateTime.UtcNow.Ticks;
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(
                Proto.DESIGNATION_UPGRADE, "i", new object[] { designationId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return true;
#endif
            SendFmt(Proto.DESIGNATION_UPGRADE, "i", designationId);
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
            _upgradeRefreshPendingId = 0;
            _wearRefreshPendingId = 0;
            EventDispatcher.Emit(GlobalEvent.EVT_DESIGNATION_LIST_UPDATE);
        }

        private void On41102(NetReader reader)
        {
            uint code = reader.ReadU32();
            uint responseId = reader.ReadU32();
            uint requestedId = _wearRequestedId;
            ClearWearPending();
            DesignationModel.Instance.ReplaceWearResult(code, responseId, false);
            if (code == 1)
            {
                _wearRefreshPendingId = responseId != 0 ? responseId : requestedId;
                TipsManager.Toast("称号佩戴成功");
                RequestStartup();
            }
            else
            {
                TipsManager.Toast("称号佩戴失败(" + code + ")");
            }
            EventDispatcher.Emit(GlobalEvent.EVT_DESIGNATION_WEAR_RESULT);
        }

        private void On41103(NetReader reader)
        {
            uint code = reader.ReadU32();
            uint requestedId = _wearRequestedId;
            ClearWearPending();
            DesignationModel.Instance.ReplaceWearResult(code, requestedId, true);
            if (code == 1)
            {
                _wearRefreshPendingId = requestedId;
                TipsManager.Toast("称号卸下成功");
                RequestStartup();
            }
            else
            {
                TipsManager.Toast("称号卸下失败(" + code + ")");
            }
            EventDispatcher.Emit(GlobalEvent.EVT_DESIGNATION_WEAR_RESULT);
        }

        private void On41104(NetReader reader)
            => DesignationModel.Instance.ReplaceActivation(reader.ReadU32(), reader.ReadU32(), reader.ReadU32());

        private void On41105(NetReader reader)
        {
            SceneDesignationPresenter.EnsureInstalled();
            ulong playerId = unchecked((ulong)reader.ReadU64());
            uint designationId = reader.ReadU32();
            DesignationModel.Instance.ReplaceSceneNotice(playerId, designationId);
            long signedPlayerId = unchecked((long)playerId);
            if (RoleModel.Instance.RoleId == signedPlayerId && RoleModel.Instance.Figure != null)
            {
                RoleModel.Instance.Figure.SetDesignationId(designationId);
                SceneDesignationPresenter.ApplySceneNotice(playerId, designationId);
            }
            else if (!SceneManager.Instance.SetRoleDesignation(signedPlayerId, designationId))
            {
                SceneDesignationPresenter.ApplySceneNotice(playerId, designationId);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_DESIGNATION_SCENE_CHANGED,
                signedPlayerId, designationId);
        }

        private void On41106(NetReader reader)
        {
            uint code = reader.ReadU32();
            byte order = reader.ReadU8();
            uint power = reader.ReadU32();
            uint currentUsed = reader.ReadU32();
            uint designationId = reader.ReadU32();
            DesignationModel.Instance.ReplaceUpgradeResult(code, order, power, currentUsed, designationId);
            ClearUpgradePending();
            if (code == 1) _upgradeRefreshPendingId = designationId;
            EventDispatcher.Emit(GlobalEvent.EVT_DESIGNATION_UPGRADE_RESULT);
            if (code == 1)
            {
                TipsManager.Toast("称号升阶成功");
                RequestStartup();
            }
            else
            {
                TipsManager.Toast("称号升阶失败(" + code + ")");
            }
        }

        private void On41107(NetReader reader)
        {
            DesignationModel.Instance.ReplacePowerQuery(reader.ReadU32(), reader.ReadU32());
            EventDispatcher.Emit(GlobalEvent.EVT_DESIGNATION_POWER_RESULT);
        }

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

        private void RefreshWriteTimeouts()
        {
            long now = DateTime.UtcNow.Ticks;
            if (_activationPending)
            {
                long elapsed = now - _activationStartedTicks;
                if (elapsed < 0 || elapsed >= ActivationTimeoutTicks) ClearActivationPending();
            }
            if (_upgradePending)
            {
                long elapsed = now - _upgradeStartedTicks;
                if (elapsed < 0 || elapsed >= ActivationTimeoutTicks) ClearUpgradePending();
            }
            if (_wearPending)
            {
                long elapsed = now - _wearStartedTicks;
                if (elapsed < 0 || elapsed >= ActivationTimeoutTicks) ClearWearPending();
            }
        }

        private void ClearActivationPending()
        {
            _activationPending = false;
            _activationStartedTicks = 0;
        }

        private void ClearUpgradePending()
        {
            _upgradePending = false;
            _upgradeStartedTicks = 0;
        }

        private void ClearWearPending()
        {
            _wearPending = false;
            _wearStartedTicks = 0;
            _wearRequestedId = 0;
        }

        public override void Dispose()
        {
            ClearActivationPending();
            ClearUpgradePending();
            ClearWearPending();
            _activationRefreshPendingId = 0;
            _upgradeRefreshPendingId = 0;
            _wearRefreshPendingId = 0;
            DesignationModel.Instance.Reset();
            base.Dispose();
        }
    }
}
