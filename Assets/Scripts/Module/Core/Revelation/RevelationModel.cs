using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Revelation
{
    /// <summary>启示圣铠 286 家族权威快照与真实事务结果；View 只读订阅。</summary>
    public sealed class RevelationModel
    {
        public sealed class Gathering
        {
            public byte Pos { get; }
            public ushort Level { get; }
            public uint Experience { get; }
            public byte Flag { get; }
            public Gathering(byte pos, ushort level, uint exp, byte flag)
            {
                Pos = pos; Level = level; Experience = exp; Flag = flag;
            }
        }

        public sealed class Suit
        {
            public uint Star { get; }
            public uint Number { get; }
            public Suit(uint star, uint number) { Star = star; Number = number; }
        }

        public sealed class Skill
        {
            public uint SkillId { get; }
            public ushort Level { get; }
            public Skill(uint id, ushort level) { SkillId = id; Level = level; }
        }

        public readonly struct EquipResult
        {
            public readonly uint Result;
            public readonly ulong GoodsId;
            public readonly ulong OldGoodsId;
            public readonly uint TypeId;
            public readonly byte CellPos;
            public EquipResult(uint result, ulong goodsId, ulong oldGoodsId, uint typeId, byte cellPos)
            {
                Result = result; GoodsId = goodsId; OldGoodsId = oldGoodsId;
                TypeId = typeId; CellPos = cellPos;
            }
        }

        public readonly struct UnloadResult
        {
            public readonly uint Result;
            public readonly ulong GoodsId;
            public readonly ushort Cell;
            public UnloadResult(uint result, ulong goodsId, ushort cell)
            {
                Result = result; GoodsId = goodsId; Cell = cell;
            }
        }

        public static readonly RevelationModel Instance = new RevelationModel();
        private readonly List<Gathering> _gatherings = new List<Gathering>();
        private readonly List<Suit> _suits = new List<Suit>();
        private readonly List<Skill> _skills = new List<Skill>();
        private readonly IReadOnlyList<Gathering> _roGatherings;
        private readonly IReadOnlyList<Suit> _roSuits;
        private readonly IReadOnlyList<Skill> _roSkills;

        private RevelationModel()
        {
            _roGatherings = _gatherings.AsReadOnly();
            _roSuits = _suits.AsReadOnly();
            _roSkills = _skills.AsReadOnly();
        }

        public ushort MaxFigureId { get; private set; }
        public ushort CurrentFigureId { get; private set; }
        public ulong Power { get; private set; }
        public bool HasData { get; private set; }
        public uint LastErrorCode { get; private set; }
        public EquipResult LastEquipResult { get; private set; }
        public UnloadResult LastUnloadResult { get; private set; }
        public IReadOnlyList<Gathering> Gatherings => _roGatherings;
        public IReadOnlyList<Suit> Suits => _roSuits;
        public IReadOnlyList<Skill> Skills => _roSkills;
        public event Action Changed;

        public bool TryGetGathering(byte pos, out Gathering value)
        {
            for (int i = 0; i < _gatherings.Count; i++)
            {
                if (_gatherings[i].Pos != pos) continue;
                value = _gatherings[i];
                return true;
            }
            value = null;
            return false;
        }

        public void Replace(ushort maxFigureId, ushort currentFigureId, ulong power,
            List<Gathering> gatherings, List<Suit> suits, List<Skill> skills)
        {
            MaxFigureId = maxFigureId;
            CurrentFigureId = currentFigureId;
            Power = power;
            _gatherings.Clear();
            _suits.Clear();
            _skills.Clear();
            if (gatherings != null) _gatherings.AddRange(gatherings);
            if (suits != null) _suits.AddRange(suits);
            if (skills != null) _skills.AddRange(skills);
            HasData = true;
            Changed?.Invoke();
        }

        public void ReplacePowerIfLoaded(ulong power)
        {
            if (!HasData) return;
            Power = power;
            Changed?.Invoke();
        }

        public void ApplyError(uint code)
        {
            LastErrorCode = code;
            Changed?.Invoke();
        }

        public void ApplyEquip(uint result, ulong goodsId, ulong oldGoodsId, uint typeId, byte cellPos)
        {
            LastEquipResult = new EquipResult(result, goodsId, oldGoodsId, typeId, cellPos);
            Changed?.Invoke();
        }

        public void ApplyUnload(uint result, ulong goodsId, ushort cell)
        {
            LastUnloadResult = new UnloadResult(result, goodsId, cell);
            Changed?.Invoke();
        }

        public void ApplyGathering(byte pos, ushort level, uint experience)
        {
            int index = _gatherings.FindIndex(item => item.Pos == pos);
            byte flag = index >= 0 ? _gatherings[index].Flag : (byte)0;
            var value = new Gathering(pos, level, experience, flag);
            if (index >= 0) _gatherings[index] = value;
            else _gatherings.Add(value);
            Changed?.Invoke();
        }

        public void ApplySkill(uint skillId, ushort level)
        {
            int index = _skills.FindIndex(item => item.SkillId == skillId);
            var value = new Skill(skillId, level);
            if (index >= 0) _skills[index] = value;
            else _skills.Add(value);
            Changed?.Invoke();
        }

        public void ApplyFigure(ushort maxFigureId, ushort currentFigureId)
        {
            MaxFigureId = maxFigureId;
            CurrentFigureId = currentFigureId;
            Changed?.Invoke();
        }

        public void Reset()
        {
            MaxFigureId = 0;
            CurrentFigureId = 0;
            Power = 0;
            LastErrorCode = 0;
            LastEquipResult = default;
            LastUnloadResult = default;
            _gatherings.Clear();
            _suits.Clear();
            _skills.Clear();
            HasData = false;
            Changed?.Invoke();
        }
    }
}
