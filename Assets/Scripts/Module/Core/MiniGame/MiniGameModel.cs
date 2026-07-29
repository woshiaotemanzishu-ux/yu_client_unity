using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.MiniGame
{
    public sealed class MiniGameModel
    {
        public sealed class StartNoticeSnapshot
        {
            public uint Code { get; }
            public byte GameType { get; }
            public ushort ModuleId { get; }
            public byte SubId { get; }
            public uint StartTime { get; }
            public uint EndTime { get; }
            public IReadOnlyList<uint> Info { get; }

            public StartNoticeSnapshot(uint code, byte gameType, ushort moduleId, byte subId,
                uint startTime, uint endTime, IReadOnlyList<uint> info)
            {
                Code = code;
                GameType = gameType;
                ModuleId = moduleId;
                SubId = subId;
                StartTime = startTime;
                EndTime = endTime;
                Info = Freeze(info);
            }
        }

        public sealed class CurrentSnapshot
        {
            public byte GameType { get; }
            public ushort ModuleId { get; }
            public byte SubId { get; }
            public uint StartTime { get; }
            public uint EndTime { get; }
            public IReadOnlyList<uint> Info { get; }

            public CurrentSnapshot(byte gameType, ushort moduleId, byte subId,
                uint startTime, uint endTime, IReadOnlyList<uint> info)
            {
                GameType = gameType;
                ModuleId = moduleId;
                SubId = subId;
                StartTime = startTime;
                EndTime = endTime;
                Info = Freeze(info);
            }
        }

        public readonly struct RankKey : IEquatable<RankKey>
        {
            public byte GameType { get; }
            public ushort ModuleId { get; }
            public byte SubId { get; }

            public RankKey(byte gameType, ushort moduleId, byte subId)
            {
                GameType = gameType;
                ModuleId = moduleId;
                SubId = subId;
            }

            public bool Equals(RankKey other) => GameType == other.GameType
                && ModuleId == other.ModuleId && SubId == other.SubId;
            public override bool Equals(object obj) => obj is RankKey other && Equals(other);
            public override int GetHashCode() => ((GameType * 397) ^ ModuleId) * 397 ^ SubId;
        }

        public sealed class RankEntry
        {
            public uint ServerId { get; }
            public uint ServerNumber { get; }
            public ushort Rank { get; }
            public ulong RoleId { get; }
            public string Name { get; }
            public uint Score { get; }

            public RankEntry(uint serverId, uint serverNumber, ushort rank, ulong roleId, string name, uint score)
            {
                ServerId = serverId;
                ServerNumber = serverNumber;
                Rank = rank;
                RoleId = roleId;
                Name = name;
                Score = score;
            }
        }

        public sealed class RankSnapshot
        {
            public RankKey Key { get; }
            public IReadOnlyList<RankEntry> Entries { get; }
            public RankSnapshot(RankKey key, IReadOnlyList<RankEntry> entries)
            {
                Key = key;
                Entries = Freeze(entries);
            }
        }

        public sealed class BoardRow
        {
            public byte RowId { get; }
            public IReadOnlyList<byte> Notes { get; }
            public BoardRow(byte rowId, IReadOnlyList<byte> notes)
            {
                RowId = rowId;
                Notes = Freeze(notes);
            }
        }

        public sealed class EffectEntry
        {
            public byte X { get; }
            public byte Y { get; }
            public byte EffectType { get; }
            public byte Parameter { get; }
            public EffectEntry(byte x, byte y, byte effectType, byte parameter)
            {
                X = x;
                Y = y;
                EffectType = effectType;
                Parameter = parameter;
            }
        }

        public sealed class ScoreChessEntry
        {
            public byte NoteId { get; }
            public byte Rate { get; }
            public ScoreChessEntry(byte noteId, byte rate)
            {
                NoteId = noteId;
                Rate = rate;
            }
        }

        public sealed class ElimReconnectSnapshot
        {
            public ushort ModuleId { get; }
            public byte SubId { get; }
            public uint StartTime { get; }
            public uint EndTime { get; }
            public uint Score { get; }
            public IReadOnlyList<BoardRow> Board { get; }
            public IReadOnlyList<EffectEntry> Effects { get; }
            public IReadOnlyList<ScoreChessEntry> ScoreChess { get; }

            public ElimReconnectSnapshot(ushort moduleId, byte subId, uint startTime, uint endTime, uint score,
                IReadOnlyList<BoardRow> board, IReadOnlyList<EffectEntry> effects,
                IReadOnlyList<ScoreChessEntry> scoreChess)
            {
                ModuleId = moduleId;
                SubId = subId;
                StartTime = startTime;
                EndTime = endTime;
                Score = score;
                Board = Freeze(board);
                Effects = Freeze(effects);
                ScoreChess = Freeze(scoreChess);
            }
        }

        public static readonly MiniGameModel Instance = new MiniGameModel();

        private readonly Dictionary<RankKey, RankSnapshot> _rankByKey = new Dictionary<RankKey, RankSnapshot>();
        private readonly IReadOnlyDictionary<RankKey, RankSnapshot> _rankView;

        private MiniGameModel()
        {
            _rankView = new ReadOnlyDictionary<RankKey, RankSnapshot>(_rankByKey);
        }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public string LastErrorMessage { get; private set; }
        public StartNoticeSnapshot StartNotice { get; private set; }
        public CurrentSnapshot Current { get; private set; }
        public IReadOnlyDictionary<RankKey, RankSnapshot> Ranks => _rankView;
        public ElimReconnectSnapshot ElimReconnect { get; private set; }

        public void ReplaceError(uint errorCode, string errorMessage)
        {
            HasError = true;
            LastErrorCode = errorCode;
            LastErrorMessage = errorMessage;
        }

        public void ReplaceStartNotice(uint code, byte gameType, ushort moduleId, byte subId,
            uint startTime, uint endTime, IReadOnlyList<uint> info)
            => StartNotice = new StartNoticeSnapshot(code, gameType, moduleId, subId, startTime, endTime, info);

        public void ReplaceCurrent(byte gameType, ushort moduleId, byte subId,
            uint startTime, uint endTime, IReadOnlyList<uint> info)
            => Current = new CurrentSnapshot(gameType, moduleId, subId, startTime, endTime, info);

        public void ReplaceRank(byte gameType, ushort moduleId, byte subId, IReadOnlyList<RankEntry> entries)
        {
            var key = new RankKey(gameType, moduleId, subId);
            _rankByKey[key] = new RankSnapshot(key, entries);
        }

        public bool TryGetRank(byte gameType, ushort moduleId, byte subId, out RankSnapshot snapshot)
            => _rankByKey.TryGetValue(new RankKey(gameType, moduleId, subId), out snapshot);

        public void ReplaceElimReconnect(ushort moduleId, byte subId, uint startTime, uint endTime, uint score,
            IReadOnlyList<BoardRow> board, IReadOnlyList<EffectEntry> effects,
            IReadOnlyList<ScoreChessEntry> scoreChess)
            => ElimReconnect = new ElimReconnectSnapshot(
                moduleId, subId, startTime, endTime, score, board, effects, scoreChess);

        public void Reset()
        {
            HasError = false;
            LastErrorCode = 0;
            LastErrorMessage = null;
            StartNotice = null;
            Current = null;
            _rankByKey.Clear();
            ElimReconnect = null;
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
