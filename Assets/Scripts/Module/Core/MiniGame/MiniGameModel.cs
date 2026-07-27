namespace Shenxiao.Module.Core.MiniGame
{
    public sealed class MiniGameModel
    {
        public static readonly MiniGameModel Instance = new MiniGameModel();

        private MiniGameModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public string LastErrorMessage { get; private set; }

        public void ReplaceError(uint errorCode, string errorMessage)
        {
            HasError = true;
            LastErrorCode = errorCode;
            LastErrorMessage = errorMessage;
        }

        public void Reset()
        {
            HasError = false;
            LastErrorCode = 0;
            LastErrorMessage = null;
        }
    }
}
