namespace Shenxiao.Module.Core.NoonParty
{
    public sealed class NoonPartyModel
    {
        public static readonly NoonPartyModel Instance = new NoonPartyModel();

        private NoonPartyModel() { }

        public bool HasData { get; private set; }
        public uint TotalExp { get; private set; }

        public void Replace(uint totalExp)
        {
            TotalExp = totalExp;
            HasData = true;
        }

        public void Reset()
        {
            TotalExp = 0;
            HasData = false;
        }
    }
}
