namespace Shenxiao.Module.Core.Pray
{
    public sealed class PrayModel
    {
        public static readonly PrayModel Instance = new PrayModel();

        private PrayModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }

        public void ReplaceError(uint errorCode)
        {
            HasError = true;
            LastErrorCode = errorCode;
        }

        public void Reset()
        {
            HasError = false;
            LastErrorCode = 0;
        }
    }
}
