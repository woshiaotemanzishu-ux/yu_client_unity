namespace Shenxiao.Module.Core.HotPoint
{
    public sealed class HotPointModel
    {
        public static readonly HotPointModel Instance = new HotPointModel();

        private HotPointModel() { }

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
