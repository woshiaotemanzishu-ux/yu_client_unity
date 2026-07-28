namespace Shenxiao.Module.Core.HolySeal
{
    public sealed class HolySealModel
    {
        public static readonly HolySealModel Instance = new HolySealModel();

        private HolySealModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public string LastErrorArgs { get; private set; }
        public bool HasRating { get; private set; }
        public uint TotalRating { get; private set; }

        public void ReplaceError(uint errorCode, string errorArgs)
        {
            HasError = true;
            LastErrorCode = errorCode;
            LastErrorArgs = errorArgs;
        }

        public void ReplaceRating(uint totalRating)
        {
            HasRating = true;
            TotalRating = totalRating;
        }

        public void Reset()
        {
            HasError = false;
            LastErrorCode = 0;
            LastErrorArgs = null;
            HasRating = false;
            TotalRating = 0;
        }
    }
}
