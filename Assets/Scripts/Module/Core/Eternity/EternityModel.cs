namespace Shenxiao.Module.Core.Eternity
{
    public sealed class EternityModel
    {
        public static readonly EternityModel Instance = new EternityModel();

        private EternityModel() { }

        public uint OpenTime { get; private set; }
        public uint EnterTime { get; private set; }
        public uint EndTime { get; private set; }
        public bool HasData { get; private set; }

        public void Replace(uint openTime, uint enterTime, uint endTime)
        {
            OpenTime = openTime;
            EnterTime = enterTime;
            EndTime = endTime;
            HasData = true;
        }

        public void Reset()
        {
            OpenTime = 0;
            EnterTime = 0;
            EndTime = 0;
            HasData = false;
        }
    }
}
