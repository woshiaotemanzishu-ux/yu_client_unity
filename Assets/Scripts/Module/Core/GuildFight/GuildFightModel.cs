namespace Shenxiao.Module.Core.GuildFight
{
    public sealed class GuildFightModel
    {
        public static readonly GuildFightModel Instance = new GuildFightModel();

        private GuildFightModel() { }

        public bool HasEnterResult { get; private set; }
        public uint EnterResultCode { get; private set; }
        public byte EnterResultType { get; private set; }

        public void ReplaceEnterResult(uint errorCode, byte type)
        {
            HasEnterResult = true;
            EnterResultCode = errorCode;
            EnterResultType = type;
        }

        public void Reset()
        {
            HasEnterResult = false;
            EnterResultCode = 0;
            EnterResultType = 0;
        }
    }
}
