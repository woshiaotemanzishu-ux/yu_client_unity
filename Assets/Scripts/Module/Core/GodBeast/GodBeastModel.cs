using System.Collections.Generic;

namespace Shenxiao.Module.Core.GodBeast
{
    /// <summary>17301 幻兽总览快照；仅保持服务端列表顺序，不做配置、装备或战力派生。</summary>
    public sealed class GodBeastModel
    {
        public sealed class Equip { public byte Position { get; } public ulong GoodsId { get; } public ushort Strengthen { get; } public uint Exp { get; } public Equip(byte position, ulong goodsId, ushort strengthen, uint exp) { Position = position; GoodsId = goodsId; Strengthen = strengthen; Exp = exp; } }
        public sealed class Attr { public ushort Type { get; } public uint Value { get; } public Attr(ushort type, uint value) { Type = type; Value = value; } }
        public sealed class Beast { public uint Id { get; } public byte State { get; } public uint Score { get; } public IReadOnlyList<Equip> Equips { get; } public IReadOnlyList<Attr> Attrs { get; } public Beast(uint id, byte state, uint score, List<Equip> equips, List<Attr> attrs) { Id = id; State = state; Score = score; Equips = equips ?? new List<Equip>(); Attrs = attrs ?? new List<Attr>(); } }
        public static readonly GodBeastModel Instance = new GodBeastModel(); private readonly List<Beast> _beasts = new List<Beast>(); private GodBeastModel() { }
        public byte FightCount { get; private set; } public IReadOnlyList<Beast> Beasts => _beasts; public bool HasData { get; private set; }
        public void ReplaceData(byte fightCount, List<Beast> beasts) { FightCount = fightCount; _beasts.Clear(); if (beasts != null) _beasts.AddRange(beasts); HasData = true; }
        public void Reset() { FightCount = 0; _beasts.Clear(); HasData = false; }
    }
}
