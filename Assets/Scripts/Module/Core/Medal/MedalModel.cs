namespace Shenxiao.Module.Core.Medal
{
    /// <summary>13401 勋章基础快照；不驱动角色战力、UI、红点或后续勋章协议。</summary>
    public sealed class MedalModel
    {
        public static readonly MedalModel Instance = new MedalModel();
        private MedalModel() { }
        public uint Id { get; private set; }
        public uint StrengthenLevel { get; private set; }
        public uint StrengthenExp { get; private set; }
        public ulong Honour { get; private set; }
        public uint Power { get; private set; }
        public uint PassLayers { get; private set; }
        public bool HasData { get; private set; }

        public void ReplaceData(uint id, uint strengthenLevel, uint strengthenExp, ulong honour, uint power, uint passLayers)
        {
            Id = id;
            StrengthenLevel = strengthenLevel;
            StrengthenExp = strengthenExp;
            Honour = honour;
            Power = power;
            PassLayers = passLayers;
            HasData = true;
        }

        public void Reset()
        {
            Id = StrengthenLevel = StrengthenExp = Power = PassLayers = 0;
            Honour = 0;
            HasData = false;
        }
    }
}
