using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.DiamondFight
{
    /// <summary>灵玉大战137家族安全读侧；报名、买命、退出、激活AI、技能、竞猜和领奖操作不接。</summary>
    public sealed class DiamondFightController : BaseController
    {
        public static readonly DiamondFightController Instance = new DiamondFightController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        public const string ICON_TYPE = DiamondFightModel.ICON_TYPE;

        private int _lastLevel = -1;

        private DiamondFightController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.DIAMOND_FIGHT_STAGE, On13700);
            RegisterProtocal(Proto.DIAMOND_FIGHT_SIGN, On13701);
            RegisterProtocal(Proto.DIAMOND_FIGHT_COUNTDOWN, On13703);
            RegisterProtocal(Proto.DIAMOND_FIGHT_ENTER_RESULT, On13704);
            RegisterProtocal(Proto.DIAMOND_FIGHT_WAITING, On13705);
            RegisterProtocal(Proto.DIAMOND_FIGHT_BATTLE_RESULT, On13708);
            RegisterProtocal(Proto.DIAMOND_FIGHT_LIVES, On13710);
            RegisterProtocal(Proto.DIAMOND_FIGHT_HISTORY, On13711);
            RegisterProtocal(Proto.DIAMOND_FIGHT_FAKE_ROLE, On13714);
            RegisterProtocal(Proto.DIAMOND_FIGHT_ZONE, On13716);
            RegisterProtocal(Proto.DIAMOND_FIGHT_UPDATE_NOTICE, On13718);
            RegisterProtocal(Proto.DIAMOND_FIGHT_BETTING, On13719);
            RegisterProtocal(Proto.DIAMOND_FIGHT_BET_RECORDS, On13721);
            RegisterProtocal(Proto.DIAMOND_FIGHT_BET_RECORD_DELTA, On13722);
            RegisterProtocal(Proto.DIAMOND_FIGHT_WINNER, On13724);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        /// <summary>GAME_START清状态后严格空发13700→13703→13716→13721。</summary>
        public void RequestStartup()
        {
            DiamondFightModel.Instance.Reset();
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            RoleModel role = RoleModel.Instance;
            _lastLevel = role.HasBaseInfo ? role.Level : -1;
            RequestStartupPackets();
        }

        public void RequestStage() => SendRequest(Proto.DIAMOND_FIGHT_STAGE);
        public void RequestSign() => SendRequest(Proto.DIAMOND_FIGHT_SIGN);
        public void RequestCountdown() => SendRequest(Proto.DIAMOND_FIGHT_COUNTDOWN);
        public void RequestLives() => SendRequest(Proto.DIAMOND_FIGHT_LIVES);
        public void RequestHistory(byte warNumber) =>
            SendRequest(Proto.DIAMOND_FIGHT_HISTORY, "c", warNumber);
        public void RequestZone() => SendRequest(Proto.DIAMOND_FIGHT_ZONE);
        public void RequestBetting() => SendRequest(Proto.DIAMOND_FIGHT_BETTING);
        public void RequestBetRecords() => SendRequest(Proto.DIAMOND_FIGHT_BET_RECORDS);

        private void RequestStartupPackets()
        {
            RequestStage();
            RequestCountdown();
            RequestZone();
            RequestBetRecords();
        }

        private void SendRequest(int protoId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId, format, args);
        }

        private void On13700(NetReader r)
        {
            byte warState = r.ReadU8();
            uint endTime = r.ReadU32();
            DiamondFightModel model = DiamondFightModel.Instance;
            model.ReplaceStage(warState, endTime);

            // 旧端在活动状态回包后补查报名；非报名/结束阶段还补查小阶段。
            if (warState == 1)
            {
                RequestSign();
            }
            else if (warState != 0 && warState != 5)
            {
                RequestSign();
                RequestCountdown();
            }

            RefreshIcon();
            GameLog.Info("DiamondFight", "13700 灵玉大战: war_state={0} end_time={1} open={2}",
                warState, endTime, model.GetIconOpenState());
        }

        private void On13701(NetReader r)
        {
            DiamondFightModel.Instance.ReplaceSign(r.ReadU8());
            RefreshIcon();
        }

        private void On13703(NetReader r) =>
            DiamondFightModel.Instance.ReplaceCountdown(new DiamondFightModel.CountdownSnapshot(
                r.ReadU8(), r.ReadU8(), r.ReadU32()));

        private void On13704(NetReader r)
        {
            uint code = r.ReadU32();
            DiamondFightModel.Instance.ReplaceEnterResult(code);
            if (code != 1)
            {
                TipsManager.Toast("操作失败(" + code + ")");
                GameLog.Warn("DiamondFight", "13704 进入准备场景失败 code={0}", code);
            }
        }

        private void On13705(NetReader r) =>
            DiamondFightModel.Instance.ReplaceWaiting(new DiamondFightModel.WaitingSnapshot(
                r.ReadU8(), r.ReadU8(), r.ReadU8(), r.ReadU8(), r.ReadU8(), r.ReadU8()));

        private void On13708(NetReader r) =>
            DiamondFightModel.Instance.ReplaceBattleResult(new DiamondFightModel.BattleResultSnapshot(
                r.ReadU8(), r.ReadU8(), r.ReadU8()));

        private void On13710(NetReader r) =>
            DiamondFightModel.Instance.ReplaceLives(new DiamondFightModel.LivesSnapshot(
                r.ReadU8(), r.ReadU8()));

        private void On13711(NetReader r)
        {
            byte warNumber = r.ReadU8();
            int count = r.ReadU16();
            var entries = new List<DiamondFightModel.HistoryEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new DiamondFightModel.HistoryEntry(
                    r.ReadU8(), r.ReadU8(), unchecked((ulong)r.ReadU64()), r.ReadU32(), r.ReadString(),
                    r.ReadU32(), r.ReadString(), r.ReadString(), r.ReadU8(),
                    unchecked((ulong)r.ReadU64()), r.ReadU8()));
            }
            DiamondFightModel.Instance.ReplaceHistory(
                new DiamondFightModel.HistorySnapshot(warNumber, entries));
        }

        private void On13714(NetReader r) =>
            DiamondFightModel.Instance.ReplaceFakeRole(new DiamondFightModel.FakeRoleSnapshot(
                unchecked((ulong)r.ReadU64()), r.ReadU32(), r.ReadU32(), r.ReadString()));

        private void On13716(NetReader r) => DiamondFightModel.Instance.ReplaceZone(r.ReadU8());

        private void On13718(NetReader r) =>
            DiamondFightModel.Instance.ReplaceUpdateNotice(new DiamondFightModel.UpdateNoticeSnapshot(
                r.ReadU32(), r.ReadU8()));

        private void On13719(NetReader r)
        {
            uint endTime = r.ReadU32();
            int actionCount = r.ReadU16();
            var actions = new List<DiamondFightModel.BettingAction>(actionCount);
            for (int i = 0; i < actionCount; i++)
            {
                byte actionId = r.ReadU8();
                int matchCount = r.ReadU16();
                var matches = new List<DiamondFightModel.MatchEntry>(matchCount);
                for (int j = 0; j < matchCount; j++) matches.Add(ReadMatch(r));
                actions.Add(new DiamondFightModel.BettingAction(actionId, matches));
            }
            DiamondFightModel.Instance.ReplaceBetting(
                new DiamondFightModel.BettingSnapshot(endTime, actions));
        }

        private void On13721(NetReader r)
        {
            int count = r.ReadU16();
            var records = new List<DiamondFightModel.BetRecord>(count);
            for (int i = 0; i < count; i++) records.Add(ReadBetRecord(r));
            DiamondFightModel.Instance.ReplaceBetRecords(
                new DiamondFightModel.BetRecordsSnapshot(records));
        }

        private void On13722(NetReader r) =>
            DiamondFightModel.Instance.ApplyRecordDelta(ReadBetRecord(r));

        private void On13724(NetReader r) =>
            DiamondFightModel.Instance.ApplyWinner(new DiamondFightModel.WinnerSnapshot(
                r.ReadU8(), r.ReadU8(), unchecked((ulong)r.ReadU64())));

        private static DiamondFightModel.MatchEntry ReadMatch(NetReader r) =>
            new DiamondFightModel.MatchEntry(
                unchecked((ulong)r.ReadU64()),
                unchecked((ulong)r.ReadU64()), r.ReadU16(), r.ReadU16(), r.ReadString(), r.ReadString(),
                r.ReadU8(), r.ReadU32(), r.ReadU8(), unchecked((ulong)r.ReadU64()),
                unchecked((ulong)r.ReadU64()), r.ReadU16(), r.ReadU16(), r.ReadString(), r.ReadString(),
                r.ReadU8(), r.ReadU32(), r.ReadU8(), unchecked((ulong)r.ReadU64()),
                unchecked((ulong)r.ReadU64()));

        private static DiamondFightModel.BetRecord ReadBetRecord(NetReader r) =>
            new DiamondFightModel.BetRecord(
                r.ReadU8(), r.ReadU8(), unchecked((ulong)r.ReadU64()), r.ReadU8(), r.ReadU8(),
                unchecked((ulong)r.ReadU64()),
                unchecked((ulong)r.ReadU64()), r.ReadU16(), r.ReadU16(), r.ReadString(), r.ReadU32(),
                r.ReadU8(), r.ReadU8(), r.ReadString(), r.ReadU8(), unchecked((ulong)r.ReadU64()),
                unchecked((ulong)r.ReadU64()), r.ReadU16(), r.ReadU16(), r.ReadString(), r.ReadU32(),
                r.ReadU8(), r.ReadU8(), r.ReadString(), r.ReadU8(), unchecked((ulong)r.ReadU64()));

        private static void RefreshIcon()
        {
            DiamondFightModel model = DiamondFightModel.Instance;
            if (model.GetIconOpenState())
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, model.GetIconText());
            else
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            // 本仓缺config_drumwar_value，沿用既有受控简化：真等级变化时重发四个只读启动查询，不硬编码开启等级。
            RequestStartupPackets();
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            DiamondFightModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }
    }
}
