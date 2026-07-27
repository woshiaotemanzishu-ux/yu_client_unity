namespace Shenxiao.Module.Core.HolyTerritory
{
    public sealed class HolyTerritoryModel
    {
        public static readonly HolyTerritoryModel Instance = new HolyTerritoryModel();

        private HolyTerritoryModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }

        public void SetError(uint code)
        {
            HasError = true;
            LastErrorCode = code;
        }

        public void Reset()
        {
            HasError = false;
            LastErrorCode = 0;
        }
    }
}
