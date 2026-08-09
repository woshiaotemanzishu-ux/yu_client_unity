using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Achievement
{
    /// <summary>
    /// 成就 409 家族。浏览快照与领奖事务都只消费服务端权威回包；40902/40905 做单飞，防止按钮连点重复提交。
    /// </summary>
    public sealed class AchievementController : BaseController
    {
        private const int ClaimTimeoutMs = 12000;

        public static readonly AchievementController Instance = new AchievementController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private bool _stageClaimPending;
        private bool _stageClaimAwaitingRefresh;
        private uint _pendingStage;
        private uint _pendingEntryId;
        private byte _pendingEntryCategory;
        private bool _entryClaimAwaitingRefresh;
        private int _stageClaimEpoch;
        private int _entryClaimEpoch;

        private AchievementController() { }

        public bool IsStageClaimPending => _stageClaimPending;
        public bool IsEntryClaimPending => _pendingEntryId != 0;

        protected override void Register()
        {
            RegisterProtocal(Proto.ACHIEVEMENT_STAGE, On40901);
            RegisterProtocal(Proto.ACHIEVEMENT_STAGE_CLAIM, On40902);
            RegisterProtocal(Proto.ACHIEVEMENT_ENTRIES, On40903);
            RegisterProtocal(Proto.ACHIEVEMENT_ENTRY_UPDATES, On40904);
            RegisterProtocal(Proto.ACHIEVEMENT_ENTRY_CLAIM, On40905);
            RegisterProtocal(Proto.ACHIEVEMENT_STAR, On40906);
            RegisterProtocal(Proto.ACHIEVEMENT_STAGE_REWARD_UPDATE, On40907);
            RegisterProtocal(Proto.ACHIEVEMENT_TYPES, On40908);
            RegisterProtocal(Proto.ACHIEVEMENT_CATEGORY_ENTRIES, On40909);
        }

        public void RequestStartup()
        {
            SendEmpty(Proto.ACHIEVEMENT_STAGE);
            SendEmpty(Proto.ACHIEVEMENT_ENTRIES);
            SendEmpty(Proto.ACHIEVEMENT_STAR);
            SendEmpty(Proto.ACHIEVEMENT_TYPES);
        }

        public void RequestCategory(ushort category)
        {
            if (category == 0 || category > byte.MaxValue) return;
            if (Intercept(Proto.ACHIEVEMENT_CATEGORY_ENTRIES, "h", category)) return;
            SendFmt(Proto.ACHIEVEMENT_CATEGORY_ENTRIES, "h", category);
        }

        public bool RequestStageClaim(uint stage)
        {
            if (stage == 0 || _stageClaimPending) return false;
            _stageClaimPending = true;
            _stageClaimAwaitingRefresh = false;
            _pendingStage = stage;
            int epoch = ++_stageClaimEpoch;
            if (!Intercept(Proto.ACHIEVEMENT_STAGE_CLAIM, "i", stage))
                SendFmt(Proto.ACHIEVEMENT_STAGE_CLAIM, "i", stage);
            _ = ReleaseStageClaimTimeoutAsync(epoch, stage);
            return true;
        }

        public bool RequestEntryClaim(uint id, byte category)
        {
            if (id == 0 || _pendingEntryId != 0) return false;
            _pendingEntryId = id;
            _pendingEntryCategory = category;
            _entryClaimAwaitingRefresh = false;
            int epoch = ++_entryClaimEpoch;
            if (!Intercept(Proto.ACHIEVEMENT_ENTRY_CLAIM, "i", id))
                SendFmt(Proto.ACHIEVEMENT_ENTRY_CLAIM, "i", id);
            _ = ReleaseEntryClaimTimeoutAsync(epoch, id);
            return true;
        }

        private void SendEmpty(int id)
        {
            if (Intercept(id, null)) return;
            SendFmt(id);
        }

        private static bool Intercept(int id, string format, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(id, format, args == null || args.Length == 0 ? null : args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return true;
#endif
            return false;
        }

        private void On40901(NetReader r)
        {
            byte stage = r.ReadU8();
            int count = r.ReadU16();
            var rewards = new List<AchievementModel.Reward>(count);
            for (int i = 0; i < count; i++)
                rewards.Add(new AchievementModel.Reward(r.ReadU32(), r.ReadU8()));
            ushort nextStage = r.ReadU16();
            if (_stageClaimAwaitingRefresh && StageSnapshotConfirmsClaim(stage)) ClearStageClaim();
            AchievementModel.Instance.ReplaceStage(stage, rewards, nextStage);
        }

        private void On40902(NetReader r)
        {
            r.ReadU8(); // cur_stage；完整阶段状态由随后 40901/40907 权威刷新
            bool success = r.ReadU8() == 1;
            uint errorCode = r.ReadU32();
            r.ReadU16(); // new_cur_stage
            uint target = _pendingStage;
            if (success) _stageClaimAwaitingRefresh = true;
            else ClearStageClaim();
            AchievementModel.Instance.NotifyOperation(
                AchievementModel.OperationKind.StageClaim, target, success, errorCode);
            if (!success) return;
            SendEmpty(Proto.ACHIEVEMENT_STAGE);
            SendEmpty(Proto.ACHIEVEMENT_STAR);
        }

        private void On40903(NetReader r)
        {
            int count = r.ReadU16();
            var entries = new List<AchievementModel.Entry>(count);
            for (int i = 0; i < count; i++)
                entries.Add(new AchievementModel.Entry(
                    r.ReadU8(), r.ReadU32(), unchecked((ulong)r.ReadU64()), r.ReadU8()));
            if (_entryClaimAwaitingRefresh && EntrySnapshotConfirmsClaim(entries)) ClearEntryClaim();
            AchievementModel.Instance.ReplaceEntries(entries);
        }

        private void On40904(NetReader r)
        {
            int count = r.ReadU16();
            var updates = new List<AchievementModel.EntryUpdate>(count);
            for (int i = 0; i < count; i++)
                updates.Add(new AchievementModel.EntryUpdate(
                    r.ReadU32(), r.ReadU8(), unchecked((ulong)r.ReadU64())));
            AchievementModel.Instance.ApplyEntryUpdates(updates);
        }

        private void On40905(NetReader r)
        {
            bool success = r.ReadU8() == 1;
            uint errorCode = r.ReadU32();
            uint id = _pendingEntryId;
            byte category = _pendingEntryCategory;
            if (success) _entryClaimAwaitingRefresh = true;
            else ClearEntryClaim();
            AchievementModel.Instance.NotifyOperation(
                AchievementModel.OperationKind.EntryClaim, id, success, errorCode);
            if (!success) return;
            RequestStartup();
            if (category != 0) RequestCategory(category);
        }

        private void On40906(NetReader r) => AchievementModel.Instance.ReplaceStar(r.ReadU32());

        private void On40907(NetReader r)
        {
            int count = r.ReadU16();
            var updates = new List<AchievementModel.Reward>(count);
            for (int i = 0; i < count; i++)
                updates.Add(new AchievementModel.Reward(r.ReadU32(), r.ReadU8()));
            byte stage = r.ReadU8();
            ushort nextStage = r.ReadU16();
            if (_stageClaimAwaitingRefresh && StageSnapshotConfirmsClaim(stage)) ClearStageClaim();
            AchievementModel.Instance.ApplyStageRewardUpdate(updates, stage, nextStage);
        }

        private void On40908(NetReader r)
        {
            int count = r.ReadU16();
            var types = new List<AchievementModel.TypeStar>(count);
            for (int i = 0; i < count; i++)
                types.Add(new AchievementModel.TypeStar(r.ReadU16(), r.ReadU32(), r.ReadU32()));
            AchievementModel.Instance.ReplaceTypes(types);
        }

        private void On40909(NetReader r)
        {
            byte category = r.ReadU8();
            int count = r.ReadU16();
            var entries = new List<AchievementModel.Entry>(count);
            for (int i = 0; i < count; i++)
                entries.Add(new AchievementModel.Entry(
                    category, r.ReadU32(), unchecked((ulong)r.ReadU64()), r.ReadU8()));
            if (_entryClaimAwaitingRefresh && category == _pendingEntryCategory
                && EntrySnapshotConfirmsClaim(entries)) ClearEntryClaim();
            AchievementModel.Instance.ReplaceCategory(category, entries);
        }

        private bool EntrySnapshotConfirmsClaim(IReadOnlyList<AchievementModel.Entry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Id == _pendingEntryId) return entries[i].Status != 1;
            return true;
        }

        private bool StageSnapshotConfirmsClaim(byte stage)
            => _pendingStage == 0 || stage >= _pendingStage;

        private async Task ReleaseStageClaimTimeoutAsync(int epoch, uint stage)
        {
            await TimeUtil.Delay(ClaimTimeoutMs);
            if (epoch != _stageClaimEpoch || !_stageClaimPending || _pendingStage != stage) return;
            GameLog.Warn("Achievement", "40902 transaction timed out stage={0}; release single-flight gate", stage);
            ClearStageClaim();
            AchievementModel.Instance.NotifyTransactionGateChanged();
        }

        private async Task ReleaseEntryClaimTimeoutAsync(int epoch, uint id)
        {
            await TimeUtil.Delay(ClaimTimeoutMs);
            if (epoch != _entryClaimEpoch || _pendingEntryId != id) return;
            GameLog.Warn("Achievement", "40905 transaction timed out id={0}; release single-flight gate", id);
            ClearEntryClaim();
            AchievementModel.Instance.NotifyTransactionGateChanged();
        }

        private void ClearStageClaim()
        {
            _stageClaimEpoch++;
            _stageClaimPending = false;
            _stageClaimAwaitingRefresh = false;
            _pendingStage = 0;
        }

        private void ClearEntryClaim()
        {
            _entryClaimEpoch++;
            _pendingEntryId = 0;
            _pendingEntryCategory = 0;
            _entryClaimAwaitingRefresh = false;
        }

        public override void Dispose()
        {
            ClearStageClaim();
            ClearEntryClaim();
            AchievementModel.Instance.Reset();
            base.Dispose();
        }
    }
}
