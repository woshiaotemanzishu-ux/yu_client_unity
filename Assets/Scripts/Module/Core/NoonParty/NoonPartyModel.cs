namespace Shenxiao.Module.Core.NoonParty
{
    public sealed class NoonPartyModel
    {
        public static readonly NoonPartyModel Instance = new NoonPartyModel();

        private NoonPartyModel() { }

        public bool HasData { get; private set; }
        public uint TotalExp { get; private set; }
        public bool HasRebornDeadline { get; private set; }
        public uint RebornDeadline { get; private set; }
        public bool HasEndDeadline { get; private set; }
        public uint EndDeadline { get; private set; }

        public void Replace(uint totalExp)
        {
            TotalExp = totalExp;
            HasData = true;
        }

        public void ReplaceRebornDeadline(uint rebornDeadline)
        {
            RebornDeadline = rebornDeadline;
            HasRebornDeadline = true;
        }

        public void ReplaceEndDeadline(uint endDeadline)
        {
            EndDeadline = endDeadline;
            HasEndDeadline = true;
        }

        public void Reset()
        {
            TotalExp = 0;
            HasData = false;
            RebornDeadline = 0;
            HasRebornDeadline = false;
            EndDeadline = 0;
            HasEndDeadline = false;
        }
    }
}
