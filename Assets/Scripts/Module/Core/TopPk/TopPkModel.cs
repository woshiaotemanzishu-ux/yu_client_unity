namespace Shenxiao.Module.Core.TopPk
{
    public sealed class TopPkModel
    {
        public static readonly TopPkModel Instance = new TopPkModel();

        private TopPkModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public string LastErrorArgs { get; private set; }

        public void SetError(uint code, string args)
        {
            HasError = true;
            LastErrorCode = code;
            LastErrorArgs = args;
        }

        public void Reset()
        {
            HasError = false;
            LastErrorCode = 0;
            LastErrorArgs = null;
        }
    }
}
