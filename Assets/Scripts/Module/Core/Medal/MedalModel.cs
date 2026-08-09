using System;

namespace Shenxiao.Module.Core.Medal
{
    /// <summary>13400 错误、13401 勋章与13405称号独立原始状态；不驱动角色战力、UI、红点或操作协议。</summary>
    public sealed class MedalModel
    {
        public enum TitleOperationKind
        {
            Upgrade,
            Equip,
            Unequip,
        }

        public sealed class TitleEntry
        {
            public uint Id { get; }
            public ushort Level { get; }
            public uint Power { get; }
            public byte IsEquip { get; }
            public TitleEntry(uint id, ushort level, uint power, byte isEquip) { Id = id; Level = level; Power = power; IsEquip = isEquip; }
        }
        public static readonly MedalModel Instance = new MedalModel();
        private readonly System.Collections.Generic.List<TitleEntry> _titles = new System.Collections.Generic.List<TitleEntry>();
        private MedalModel() { }
        public event Action Changed;
        public event Action<uint> ErrorReceived;
        public event Action<TitleOperationKind, uint, ushort, uint> TitleOperationCompleted;
        public uint Id { get; private set; }
        public uint StrengthenLevel { get; private set; }
        public uint StrengthenExp { get; private set; }
        public ulong Honour { get; private set; }
        public uint Power { get; private set; }
        public uint PassLayers { get; private set; }
        public bool HasData { get; private set; }
        public System.Collections.Generic.IReadOnlyList<TitleEntry> TitleEntries => _titles;
        public bool HasTitleData { get; private set; }
        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }

        public void ReplaceData(uint id, uint strengthenLevel, uint strengthenExp, ulong honour, uint power, uint passLayers)
        {
            Id = id;
            StrengthenLevel = strengthenLevel;
            StrengthenExp = strengthenExp;
            Honour = honour;
            Power = power;
            PassLayers = passLayers;
            HasData = true;
            Changed?.Invoke();
        }
        /// <summary>13402 只替换回包携带的 id/honour，其余强化与副本快照字段保持不变。</summary>
        public void ApplyUpgrade(uint id, ulong honour)
        {
            Id = id;
            Honour = honour;
            HasData = true;
            Changed?.Invoke();
        }
        public void ReplaceTitles(System.Collections.Generic.List<TitleEntry> titles)
        {
            _titles.Clear();
            if (titles != null) _titles.AddRange(titles);
            HasTitleData = true;
            Changed?.Invoke();
        }
        public void NotifyTitleOperation(TitleOperationKind kind, uint id, ushort level, uint code)
        {
            TitleOperationCompleted?.Invoke(kind, id, level, code);
        }
        public void SetError(uint code)
        {
            LastErrorCode = code;
            HasError = true;
            ErrorReceived?.Invoke(code);
        }

        public void Reset()
        {
            Id = StrengthenLevel = StrengthenExp = Power = PassLayers = 0;
            Honour = 0;
            HasData = false;
            _titles.Clear();
            HasTitleData = false;
            HasError = false;
            LastErrorCode = 0;
        }
    }
}
