using System.Collections.Generic;

namespace Shenxiao.Module.Core.Guard
{
    public sealed class GuardModel
    {
        public sealed class Circle
        {
            public byte Status { get; }
            public byte Level { get; }
            public uint EndTime { get; }
            public byte Show { get; }
            public byte FreeFlag { get; }

            public Circle(byte status, byte level, uint endTime, byte show, byte freeFlag)
            {
                Status = status;
                Level = level;
                EndTime = endTime;
                Show = show;
                FreeFlag = freeFlag;
            }
        }

        public static readonly GuardModel Instance = new GuardModel();

        private readonly List<Circle> _circles = new List<Circle>();
        private readonly IReadOnlyList<Circle> _readonly;

        private GuardModel()
        {
            _readonly = _circles.AsReadOnly();
        }

        public bool HasData { get; private set; }
        public IReadOnlyList<Circle> Circles => _readonly;

        public void Replace(List<Circle> values)
        {
            _circles.Clear();
            if (values != null) _circles.AddRange(values);
            HasData = true;
        }

        public void Reset()
        {
            _circles.Clear();
            HasData = false;
        }
    }
}
