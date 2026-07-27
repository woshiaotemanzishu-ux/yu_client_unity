namespace Shenxiao.Module.Core.NoonParty
{
    public sealed class NoonPartyModel
    {
        public static readonly NoonPartyModel Instance = new NoonPartyModel();

        private NoonPartyModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public bool HasData { get; private set; }
        public uint TotalExp { get; private set; }
        public bool HasBoxCounts { get; private set; }
        public uint LowBoxCount { get; private set; }
        public uint HighBoxCount { get; private set; }
        public bool HasRebornDeadline { get; private set; }
        public uint RebornDeadline { get; private set; }
        public bool HasEndDeadline { get; private set; }
        public uint EndDeadline { get; private set; }

        public void ReplaceError(uint code)
        {
            LastErrorCode = code;
            HasError = true;
        }

        public void Replace(uint totalExp)
        {
            TotalExp = totalExp;
            HasData = true;
        }

        public void ReplaceBoxCounts(uint lowBoxCount, uint highBoxCount)
        {
            LowBoxCount = lowBoxCount;
            HighBoxCount = highBoxCount;
            HasBoxCounts = true;
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
            LastErrorCode = 0;
            HasError = false;
            TotalExp = 0;
            HasData = false;
            LowBoxCount = 0;
            HighBoxCount = 0;
            HasBoxCounts = false;
            RebornDeadline = 0;
            HasRebornDeadline = false;
            EndDeadline = 0;
            HasEndDeadline = false;
        }
    }
}
