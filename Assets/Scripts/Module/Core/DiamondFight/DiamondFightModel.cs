using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.DiamondFight
{
    /// <summary>灵玉大战137家族的原始读侧状态；查询快照、推送事件和增量各自保留。</summary>
    public sealed class DiamondFightModel
    {
        public sealed class StageSnapshot
        {
            public byte WarState { get; }
            public uint EndTime { get; }

            public StageSnapshot(byte warState, uint endTime)
            {
                WarState = warState;
                EndTime = endTime;
            }
        }

        public sealed class SignSnapshot
        {
            public byte IsSign { get; }
            public SignSnapshot(byte isSign) => IsSign = isSign;
        }

        public sealed class CountdownSnapshot
        {
            public byte Action { get; }
            public byte Type { get; }
            public uint EndTime { get; }

            public CountdownSnapshot(byte action, byte type, uint endTime)
            {
                Action = action;
                Type = type;
                EndTime = endTime;
            }
        }

        public sealed class WaitingSnapshot
        {
            public byte IsOut { get; }
            public byte Zone { get; }
            public byte Stage { get; }
            public byte WinCount { get; }
            public byte LoseCount { get; }
            public byte LifeCount { get; }

            public WaitingSnapshot(byte isOut, byte zone, byte stage, byte winCount, byte loseCount, byte lifeCount)
            {
                IsOut = isOut;
                Zone = zone;
                Stage = stage;
                WinCount = winCount;
                LoseCount = loseCount;
                LifeCount = lifeCount;
            }
        }

        public sealed class EnterResultSnapshot
        {
            public uint Code { get; }
            public EnterResultSnapshot(uint code) => Code = code;
        }

        public sealed class BattleResultSnapshot
        {
            public byte Settlement { get; }
            public byte Result { get; }
            public byte ActionId { get; }

            public BattleResultSnapshot(byte settlement, byte result, byte actionId)
            {
                Settlement = settlement;
                Result = result;
                ActionId = actionId;
            }
        }

        public sealed class LivesSnapshot
        {
            public byte SelfLife { get; }
            public byte OtherLife { get; }

            public LivesSnapshot(byte selfLife, byte otherLife)
            {
                SelfLife = selfLife;
                OtherLife = otherLife;
            }
        }

        public sealed class HistoryEntry
        {
            public byte Zone { get; }
            public byte Rank { get; }
            public ulong RoleId { get; }
            public uint ServerId { get; }
            public string Platform { get; }
            public uint PlatformId { get; }
            public string RoleName { get; }
            public string GuildName { get; }
            public byte Vip { get; }
            public ulong Power { get; }
            public byte Career { get; }

            public HistoryEntry(byte zone, byte rank, ulong roleId, uint serverId, string platform,
                uint platformId, string roleName, string guildName, byte vip, ulong power, byte career)
            {
                Zone = zone;
                Rank = rank;
                RoleId = roleId;
                ServerId = serverId;
                Platform = platform ?? string.Empty;
                PlatformId = platformId;
                RoleName = roleName ?? string.Empty;
                GuildName = guildName ?? string.Empty;
                Vip = vip;
                Power = power;
                Career = career;
            }
        }

        public sealed class HistorySnapshot
        {
            public byte WarNumber { get; }
            public IReadOnlyList<HistoryEntry> Entries { get; }

            public HistorySnapshot(byte warNumber, IReadOnlyList<HistoryEntry> entries)
            {
                WarNumber = warNumber;
                Entries = Freeze(entries);
            }
        }

        public sealed class FakeRoleSnapshot
        {
            public ulong Power { get; }
            public uint ServerId { get; }
            public uint ServerNumber { get; }
            public string ServerName { get; }

            public FakeRoleSnapshot(ulong power, uint serverId, uint serverNumber, string serverName)
            {
                Power = power;
                ServerId = serverId;
                ServerNumber = serverNumber;
                ServerName = serverName ?? string.Empty;
            }
        }

        public sealed class ZoneSnapshot
        {
            public byte Zone { get; }
            public ZoneSnapshot(byte zone) => Zone = zone;
        }

        public sealed class UpdateNoticeSnapshot
        {
            public uint EndTime { get; }
            public byte Update { get; }

            public UpdateNoticeSnapshot(uint endTime, byte update)
            {
                EndTime = endTime;
                Update = update;
            }
        }

        public sealed class MatchEntry
        {
            public ulong SupporterId { get; }
            public ulong ARoleId { get; }
            public ushort AServerId { get; }
            public ushort AServerNumber { get; }
            public string AName { get; }
            public string APicture { get; }
            public byte APictureVersion { get; }
            public uint ALevel { get; }
            public byte ACareer { get; }
            public ulong APower { get; }
            public ulong BRoleId { get; }
            public ushort BServerId { get; }
            public ushort BServerNumber { get; }
            public string BName { get; }
            public string BPicture { get; }
            public byte BPictureVersion { get; }
            public uint BLevel { get; }
            public byte BCareer { get; }
            public ulong BPower { get; }
            public ulong Winner { get; }

            public MatchEntry(ulong supporterId,
                ulong aRoleId, ushort aServerId, ushort aServerNumber, string aName, string aPicture,
                byte aPictureVersion, uint aLevel, byte aCareer, ulong aPower,
                ulong bRoleId, ushort bServerId, ushort bServerNumber, string bName, string bPicture,
                byte bPictureVersion, uint bLevel, byte bCareer, ulong bPower, ulong winner)
            {
                SupporterId = supporterId;
                ARoleId = aRoleId;
                AServerId = aServerId;
                AServerNumber = aServerNumber;
                AName = aName ?? string.Empty;
                APicture = aPicture ?? string.Empty;
                APictureVersion = aPictureVersion;
                ALevel = aLevel;
                ACareer = aCareer;
                APower = aPower;
                BRoleId = bRoleId;
                BServerId = bServerId;
                BServerNumber = bServerNumber;
                BName = bName ?? string.Empty;
                BPicture = bPicture ?? string.Empty;
                BPictureVersion = bPictureVersion;
                BLevel = bLevel;
                BCareer = bCareer;
                BPower = bPower;
                Winner = winner;
            }

            public MatchEntry WithWinner(ulong winner) => new MatchEntry(SupporterId,
                ARoleId, AServerId, AServerNumber, AName, APicture, APictureVersion, ALevel, ACareer, APower,
                BRoleId, BServerId, BServerNumber, BName, BPicture, BPictureVersion, BLevel, BCareer, BPower,
                winner);
        }

        public sealed class BettingAction
        {
            public byte ActionId { get; }
            public IReadOnlyList<MatchEntry> Matches { get; }

            public BettingAction(byte actionId, IReadOnlyList<MatchEntry> matches)
            {
                ActionId = actionId;
                Matches = Freeze(matches);
            }
        }

        public sealed class BettingSnapshot
        {
            public uint EndTime { get; }
            public IReadOnlyList<BettingAction> Actions { get; }

            public BettingSnapshot(uint endTime, IReadOnlyList<BettingAction> actions)
            {
                EndTime = endTime;
                Actions = Freeze(actions);
            }
        }

        public sealed class BetRecord
        {
            public byte Zone { get; }
            public byte Action { get; }
            public ulong SupporterId { get; }
            public byte GuessType { get; }
            public byte RewardState { get; }
            public ulong Winner { get; }
            public ulong ARoleId { get; }
            public ushort AServerId { get; }
            public ushort AServerNumber { get; }
            public string AName { get; }
            public uint ALevel { get; }
            public byte ASex { get; }
            public byte ACareer { get; }
            public string APicture { get; }
            public byte APictureVersion { get; }
            public ulong APower { get; }
            public ulong BRoleId { get; }
            public ushort BServerId { get; }
            public ushort BServerNumber { get; }
            public string BName { get; }
            public uint BLevel { get; }
            public byte BSex { get; }
            public byte BCareer { get; }
            public string BPicture { get; }
            public byte BPictureVersion { get; }
            public ulong BPower { get; }

            public BetRecord(byte zone, byte action, ulong supporterId, byte guessType, byte rewardState,
                ulong winner, ulong aRoleId, ushort aServerId, ushort aServerNumber, string aName,
                uint aLevel, byte aSex, byte aCareer, string aPicture, byte aPictureVersion, ulong aPower,
                ulong bRoleId, ushort bServerId, ushort bServerNumber, string bName, uint bLevel,
                byte bSex, byte bCareer, string bPicture, byte bPictureVersion, ulong bPower)
            {
                Zone = zone;
                Action = action;
                SupporterId = supporterId;
                GuessType = guessType;
                RewardState = rewardState;
                Winner = winner;
                ARoleId = aRoleId;
                AServerId = aServerId;
                AServerNumber = aServerNumber;
                AName = aName ?? string.Empty;
                ALevel = aLevel;
                ASex = aSex;
                ACareer = aCareer;
                APicture = aPicture ?? string.Empty;
                APictureVersion = aPictureVersion;
                APower = aPower;
                BRoleId = bRoleId;
                BServerId = bServerId;
                BServerNumber = bServerNumber;
                BName = bName ?? string.Empty;
                BLevel = bLevel;
                BSex = bSex;
                BCareer = bCareer;
                BPicture = bPicture ?? string.Empty;
                BPictureVersion = bPictureVersion;
                BPower = bPower;
            }
        }

        public sealed class BetRecordsSnapshot
        {
            public IReadOnlyList<BetRecord> Records { get; }
            public BetRecordsSnapshot(IReadOnlyList<BetRecord> records) => Records = Freeze(records);
        }

        public sealed class WinnerSnapshot
        {
            public byte Zone { get; }
            public byte Action { get; }
            public ulong Winner { get; }

            public WinnerSnapshot(byte zone, byte action, ulong winner)
            {
                Zone = zone;
                Action = action;
                Winner = winner;
            }
        }

        public static readonly DiamondFightModel Instance = new DiamondFightModel();
        public const string ICON_TYPE = "137";

        private readonly Dictionary<byte, HistorySnapshot> _historyByWar =
            new Dictionary<byte, HistorySnapshot>();

        private DiamondFightModel() { }

        public StageSnapshot Stage { get; private set; }
        public SignSnapshot Sign { get; private set; }
        public CountdownSnapshot LastCountdown { get; private set; }
        public WaitingSnapshot Waiting { get; private set; }
        public EnterResultSnapshot LastEnterResult { get; private set; }
        public BattleResultSnapshot LastBattleResult { get; private set; }
        public LivesSnapshot Lives { get; private set; }
        public IReadOnlyDictionary<byte, HistorySnapshot> Histories => _historyByWar;
        public FakeRoleSnapshot FakeRole { get; private set; }
        public ZoneSnapshot Zone { get; private set; }
        public UpdateNoticeSnapshot LastUpdateNotice { get; private set; }
        public BettingSnapshot Betting { get; private set; }
        public BetRecordsSnapshot BetRecords { get; private set; }
        public BetRecord LastRecordDelta { get; private set; }
        public WinnerSnapshot LastWinner { get; private set; }

        public bool HasStage => Stage != null;
        public bool HasSign => Sign != null;
        public bool HasCountdown => LastCountdown != null;
        public bool HasWaiting => Waiting != null;
        public bool HasEnterResult => LastEnterResult != null;
        public bool HasBattleResult => LastBattleResult != null;
        public bool HasLives => Lives != null;
        public bool HasFakeRole => FakeRole != null;
        public bool HasZone => Zone != null;
        public bool HasUpdateNotice => LastUpdateNotice != null;
        public bool HasBetting => Betting != null;
        public bool HasBetRecords => BetRecords != null;
        public bool HasRecordDelta => LastRecordDelta != null;
        public bool HasWinner => LastWinner != null;

        // 保留既有图标消费接口。
        public int WarState => Stage?.WarState ?? 0;
        public long EndTime => Stage?.EndTime ?? 0;
        public int IsSign => Sign?.IsSign ?? 0;

        public void ReplaceStage(byte warState, uint endTime)
        {
            Stage = new StageSnapshot(warState, endTime);
            // 对标旧端13700的关闭/结束分支：报名状态明确回到0，避免下轮活动沿用旧值。
            if (warState == 0 || warState == 5) Sign = new SignSnapshot(0);
        }

        public void ReplaceSign(byte isSign) => Sign = new SignSnapshot(isSign);
        public void ReplaceCountdown(CountdownSnapshot snapshot) => LastCountdown = snapshot;
        public void ReplaceWaiting(WaitingSnapshot snapshot) => Waiting = snapshot;
        public void ReplaceEnterResult(uint code) => LastEnterResult = new EnterResultSnapshot(code);
        public void ReplaceBattleResult(BattleResultSnapshot snapshot) => LastBattleResult = snapshot;
        public void ReplaceLives(LivesSnapshot snapshot) => Lives = snapshot;
        public void ReplaceHistory(HistorySnapshot snapshot) => _historyByWar[snapshot.WarNumber] = snapshot;
        public void ReplaceFakeRole(FakeRoleSnapshot snapshot) => FakeRole = snapshot;
        public void ReplaceZone(byte zone) => Zone = new ZoneSnapshot(zone);
        public void ReplaceUpdateNotice(UpdateNoticeSnapshot snapshot) => LastUpdateNotice = snapshot;
        public void ReplaceBetting(BettingSnapshot snapshot) => Betting = snapshot;
        public void ReplaceBetRecords(BetRecordsSnapshot snapshot) => BetRecords = snapshot;

        /// <summary>13722是单条新增记录；按旧端语义追加，早于13721时也建立仅含该条的已加载表。</summary>
        public void ApplyRecordDelta(BetRecord record)
        {
            LastRecordDelta = record;
            var records = new List<BetRecord>((BetRecords?.Records.Count ?? 0) + 1);
            if (BetRecords != null)
                for (int i = 0; i < BetRecords.Records.Count; i++) records.Add(BetRecords.Records[i]);
            records.Add(record);
            BetRecords = new BetRecordsSnapshot(records);
        }

        /// <summary>13724保存raw事件，并在已加载13719中更新首个包含胜者的同场次项。</summary>
        public void ApplyWinner(WinnerSnapshot winner)
        {
            LastWinner = winner;
            if (Betting == null) return;

            bool patched = false;
            var actions = new List<BettingAction>(Betting.Actions.Count);
            for (int i = 0; i < Betting.Actions.Count; i++)
            {
                BettingAction action = Betting.Actions[i];
                if (patched || action.ActionId != winner.Action)
                {
                    actions.Add(action);
                    continue;
                }

                var matches = new List<MatchEntry>(action.Matches.Count);
                for (int j = 0; j < action.Matches.Count; j++)
                {
                    MatchEntry match = action.Matches[j];
                    if (!patched && (match.ARoleId == winner.Winner || match.BRoleId == winner.Winner))
                    {
                        matches.Add(match.WithWinner(winner.Winner));
                        patched = true;
                    }
                    else
                    {
                        matches.Add(match);
                    }
                }
                actions.Add(new BettingAction(action.ActionId, matches));
            }
            if (patched) Betting = new BettingSnapshot(Betting.EndTime, actions);
        }

        public bool TryGetHistory(byte warNumber, out HistorySnapshot snapshot) =>
            _historyByWar.TryGetValue(warNumber, out snapshot);

        public bool GetIconOpenState() =>
            WarState >= 1 && WarState <= 4 && (WarState != 1 || IsSign != 1);

        public string GetIconText()
        {
            if (WarState == 1) return "报名中";
            if (WarState >= 2 && WarState <= 4) return "进行中";
            return string.Empty;
        }

        public void Reset()
        {
            Stage = null;
            Sign = null;
            LastCountdown = null;
            Waiting = null;
            LastEnterResult = null;
            LastBattleResult = null;
            Lives = null;
            _historyByWar.Clear();
            FakeRole = null;
            Zone = null;
            LastUpdateNotice = null;
            Betting = null;
            BetRecords = null;
            LastRecordDelta = null;
            LastWinner = null;
        }

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
    }
}
