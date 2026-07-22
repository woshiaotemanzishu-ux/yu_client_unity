using System.Collections.Generic;

namespace Shenxiao.Module.Core.MonBook
{
    public sealed class MonBookModel
    {
        public static readonly MonBookModel Instance = new MonBookModel();
        private readonly List<uint> _activatedPics = new List<uint>();
        private readonly IReadOnlyList<uint> _readOnlyActivatedPics;

        private MonBookModel()
        {
            _readOnlyActivatedPics = _activatedPics.AsReadOnly();
        }

        public bool HasData { get; private set; }
        public IReadOnlyList<uint> ActivatedPics => _readOnlyActivatedPics;

        public void Replace(List<uint> activatedPics)
        {
            _activatedPics.Clear();
            if (activatedPics != null) _activatedPics.AddRange(activatedPics);
            HasData = true;
        }

        public void Reset()
        {
            _activatedPics.Clear();
            HasData = false;
        }
    }
}
