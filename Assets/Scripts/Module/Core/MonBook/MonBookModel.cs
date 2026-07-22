using System.Collections.Generic;

namespace Shenxiao.Module.Core.MonBook
{
    public sealed class MonBookModel
    {
        public static readonly MonBookModel Instance = new MonBookModel();
        private readonly List<uint> _activatedPics = new List<uint>();
        private readonly IReadOnlyList<uint> _readOnlyActivatedPics;
        private readonly Dictionary<uint, ulong> _previewPowers = new Dictionary<uint, ulong>();
        private readonly IReadOnlyDictionary<uint, ulong> _readOnlyPreviewPowers;

        private MonBookModel()
        {
            _readOnlyActivatedPics = _activatedPics.AsReadOnly();
            _readOnlyPreviewPowers = new System.Collections.ObjectModel.ReadOnlyDictionary<uint, ulong>(_previewPowers);
        }

        public bool HasData { get; private set; }
        public IReadOnlyList<uint> ActivatedPics => _readOnlyActivatedPics;
        public IReadOnlyDictionary<uint, ulong> PreviewPowers => _readOnlyPreviewPowers;
        public bool TryGetPreviewPower(uint picId, out ulong power) { return _previewPowers.TryGetValue(picId, out power); }
        public void ReplacePreviewPower(uint picId, ulong power) { _previewPowers[picId] = power; }

        public void Replace(List<uint> activatedPics)
        {
            _activatedPics.Clear();
            if (activatedPics != null) _activatedPics.AddRange(activatedPics);
            HasData = true;
        }

        public void Reset()
        {
            _activatedPics.Clear();
            _previewPowers.Clear();
            HasData = false;
        }
    }
}
