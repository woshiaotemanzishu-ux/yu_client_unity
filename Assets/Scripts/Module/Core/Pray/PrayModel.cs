using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Pray
{
    public sealed class PrayModel
    {
        public static readonly PrayModel Instance = new PrayModel();

        public sealed class PrayInfo
        {
            public readonly byte Type;
            public readonly byte RemainTimes;
            public readonly byte FreeTimes;
            public readonly uint EndTime;

            public PrayInfo(byte type, byte remainTimes, byte freeTimes, uint endTime)
            {
                Type = type;
                RemainTimes = remainTimes;
                FreeTimes = freeTimes;
                EndTime = endTime;
            }
        }

        private IReadOnlyList<PrayInfo> _prayInfoList = Array.AsReadOnly(Array.Empty<PrayInfo>());

        private PrayModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public bool HasPrayInfo { get; private set; }
        public IReadOnlyList<PrayInfo> PrayInfoList => _prayInfoList;

        public void ReplaceError(uint errorCode)
        {
            HasError = true;
            LastErrorCode = errorCode;
        }

        public void ReplacePrayInfo(IReadOnlyList<PrayInfo> entries)
        {
            int count = entries?.Count ?? 0;
            var copy = new PrayInfo[count];
            for (int i = 0; i < count; i++)
            {
                PrayInfo entry = entries[i];
                copy[i] = new PrayInfo(entry.Type, entry.RemainTimes, entry.FreeTimes, entry.EndTime);
            }

            _prayInfoList = Array.AsReadOnly(copy);
            HasPrayInfo = true;
        }

        public void Reset()
        {
            HasError = false;
            LastErrorCode = 0;
            HasPrayInfo = false;
            _prayInfoList = Array.AsReadOnly(Array.Empty<PrayInfo>());
        }
    }
}
