using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.GodBeast
{
    /// <summary>幻兽原始协议状态；只保存服务端 wire 事实，不做配置、背包或战力派生。</summary>
    public sealed class GodBeastModel
    {
        public sealed class Equip
        {
            public byte Position { get; }
            public ulong GoodsId { get; }
            public ushort Strengthen { get; }
            public uint Exp { get; }

            public Equip(byte position, ulong goodsId, ushort strengthen, uint exp)
            {
                Position = position;
                GoodsId = goodsId;
                Strengthen = strengthen;
                Exp = exp;
            }
        }

        public sealed class Attr
        {
            public ushort Type { get; }
            public uint Value { get; }

            public Attr(ushort type, uint value)
            {
                Type = type;
                Value = value;
            }
        }

        public sealed class Beast
        {
            public uint Id { get; }
            public byte State { get; }
            public uint Score { get; }
            public IReadOnlyList<Equip> Equips { get; }
            public IReadOnlyList<Attr> Attrs { get; }

            public Beast(uint id, byte state, uint score, IReadOnlyList<Equip> equips, IReadOnlyList<Attr> attrs)
            {
                Id = id;
                State = state;
                Score = score;
                Equips = equips == null || equips.Count == 0
                    ? Array.Empty<Equip>()
                    : new List<Equip>(equips).AsReadOnly();
                Attrs = attrs == null || attrs.Count == 0
                    ? Array.Empty<Attr>()
                    : new List<Attr>(attrs).AsReadOnly();
            }
        }

        public static readonly GodBeastModel Instance = new GodBeastModel();

        private readonly List<Beast> _beasts = new List<Beast>();
        private readonly Dictionary<uint, uint> _attributePowers = new Dictionary<uint, uint>();

        private GodBeastModel() { }

        public byte FightCount { get; private set; }
        public IReadOnlyList<Beast> Beasts => _beasts;
        public bool HasData { get; private set; }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public string LastErrorArgs { get; private set; }

        public bool HasStrengthPreview { get; private set; }
        public ulong PreviewGoodsId { get; private set; }
        public ushort PreviewStrengthen { get; private set; }
        public uint PreviewExp { get; private set; }

        public int AttributePowerCount => _attributePowers.Count;

        public void ReplaceData(byte fightCount, IReadOnlyList<Beast> beasts)
        {
            FightCount = fightCount;
            _beasts.Clear();
            if (beasts != null)
                for (int i = 0; i < beasts.Count; i++)
                    _beasts.Add(beasts[i]);
            HasData = true;
        }

        /// <summary>对标老端 SetBeastInfoOne：只替换已加载总览中的首个同 ID 项。</summary>
        public bool ApplyBeastUpdate(Beast beast)
        {
            if (!HasData || beast == null) return false;
            for (int i = 0; i < _beasts.Count; i++)
            {
                if (_beasts[i].Id != beast.Id) continue;
                _beasts[i] = beast;
                return true;
            }
            return false;
        }

        public void SetError(uint code, string args)
        {
            HasError = true;
            LastErrorCode = code;
            LastErrorArgs = args;
        }

        public void ReplaceStrengthPreview(ulong goodsId, ushort strengthen, uint exp)
        {
            HasStrengthPreview = true;
            PreviewGoodsId = goodsId;
            PreviewStrengthen = strengthen;
            PreviewExp = exp;
        }

        public void ReplaceAttributePower(ushort moduleId, byte subModuleId, uint combatPower)
        {
            _attributePowers[AttributePowerKey(moduleId, subModuleId)] = combatPower;
        }

        public bool TryGetAttributePower(ushort moduleId, byte subModuleId, out uint combatPower)
        {
            return _attributePowers.TryGetValue(AttributePowerKey(moduleId, subModuleId), out combatPower);
        }

        public void Reset()
        {
            FightCount = 0;
            _beasts.Clear();
            HasData = false;
            HasError = false;
            LastErrorCode = 0;
            LastErrorArgs = null;
            HasStrengthPreview = false;
            PreviewGoodsId = 0;
            PreviewStrengthen = 0;
            PreviewExp = 0;
            _attributePowers.Clear();
        }

        private static uint AttributePowerKey(ushort moduleId, byte subModuleId)
        {
            return ((uint)moduleId << 8) | subModuleId;
        }
    }
}
