using System.Collections.Generic;
namespace Shenxiao.Module.Core.Revelation
{
    public sealed class RevelationModel
    {
        public sealed class Gathering { public byte Pos { get; } public ushort Level { get; } public uint Experience { get; } public byte Flag { get; } public Gathering(byte pos, ushort level, uint exp, byte flag) { Pos = pos; Level = level; Experience = exp; Flag = flag; } }
        public sealed class Suit { public uint Star { get; } public uint Number { get; } public Suit(uint star, uint number) { Star = star; Number = number; } }
        public sealed class Skill { public uint SkillId { get; } public ushort Level { get; } public Skill(uint id, ushort level) { SkillId = id; Level = level; } }
        public static readonly RevelationModel Instance = new RevelationModel(); private readonly List<Gathering> _gatherings = new List<Gathering>(); private readonly List<Suit> _suits = new List<Suit>(); private readonly List<Skill> _skills = new List<Skill>(); private readonly IReadOnlyList<Gathering> _roGatherings; private readonly IReadOnlyList<Suit> _roSuits; private readonly IReadOnlyList<Skill> _roSkills;
        private RevelationModel() { _roGatherings = _gatherings.AsReadOnly(); _roSuits = _suits.AsReadOnly(); _roSkills = _skills.AsReadOnly(); }
        public ushort MaxFigureId { get; private set; } public ushort CurrentFigureId { get; private set; } public ulong Power { get; private set; } public bool HasData { get; private set; } public IReadOnlyList<Gathering> Gatherings => _roGatherings; public IReadOnlyList<Suit> Suits => _roSuits; public IReadOnlyList<Skill> Skills => _roSkills;
        public bool TryGetGathering(byte pos, out Gathering value) { for (int i = 0; i < _gatherings.Count; i++) if (_gatherings[i].Pos == pos) { value = _gatherings[i]; return true; } value = null; return false; }
        public void Replace(ushort maxFigureId, ushort currentFigureId, ulong power, List<Gathering> gatherings, List<Suit> suits, List<Skill> skills) { MaxFigureId = maxFigureId; CurrentFigureId = currentFigureId; Power = power; _gatherings.Clear(); _suits.Clear(); _skills.Clear(); if (gatherings != null) _gatherings.AddRange(gatherings); if (suits != null) _suits.AddRange(suits); if (skills != null) _skills.AddRange(skills); HasData = true; }
        public void Reset() { MaxFigureId = 0; CurrentFigureId = 0; Power = 0; _gatherings.Clear(); _suits.Clear(); _skills.Clear(); HasData = false; }
    }
}
