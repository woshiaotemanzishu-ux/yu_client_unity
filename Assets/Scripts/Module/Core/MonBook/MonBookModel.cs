using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.MonBook
{
    public sealed class MonBookModel
    {
        public sealed class GroupEntry
        {
            public uint GroupId { get; }
            public ushort Level { get; }

            public GroupEntry(uint groupId, ushort level)
            {
                GroupId = groupId;
                Level = level;
            }
        }

        public sealed class PictureEntry
        {
            public uint PicId { get; }
            public ushort Level { get; }
            public ulong CurPower { get; }
            public ulong NextPower { get; }

            public PictureEntry(uint picId, ushort level, ulong curPower, ulong nextPower)
            {
                PicId = picId;
                Level = level;
                CurPower = curPower;
                NextPower = nextPower;
            }
        }

        public sealed class BookSnapshot
        {
            public ushort Type { get; }
            public IReadOnlyList<GroupEntry> Groups { get; }
            public IReadOnlyList<PictureEntry> Pictures { get; }
            public ulong PicCombat { get; }

            public BookSnapshot(ushort type, List<GroupEntry> groups, List<PictureEntry> pictures, ulong picCombat)
            {
                Type = type;
                Groups = new List<GroupEntry>(groups ?? new List<GroupEntry>()).AsReadOnly();
                Pictures = new List<PictureEntry>(pictures ?? new List<PictureEntry>()).AsReadOnly();
                PicCombat = picCombat;
            }
        }

        public static readonly MonBookModel Instance = new MonBookModel();

        private readonly List<uint> _activatedPics = new List<uint>();
        private readonly IReadOnlyList<uint> _readOnlyActivatedPics;
        private readonly Dictionary<uint, ulong> _previewPowers = new Dictionary<uint, ulong>();
        private readonly IReadOnlyDictionary<uint, ulong> _readOnlyPreviewPowers;
        private readonly Dictionary<ushort, BookSnapshot> _books = new Dictionary<ushort, BookSnapshot>();
        private readonly IReadOnlyDictionary<ushort, BookSnapshot> _readOnlyBooks;

        private MonBookModel()
        {
            _readOnlyActivatedPics = _activatedPics.AsReadOnly();
            _readOnlyPreviewPowers = new ReadOnlyDictionary<uint, ulong>(_previewPowers);
            _readOnlyBooks = new ReadOnlyDictionary<ushort, BookSnapshot>(_books);
        }

        public bool HasData { get; private set; }
        public IReadOnlyList<uint> ActivatedPics => _readOnlyActivatedPics;
        public IReadOnlyDictionary<uint, ulong> PreviewPowers => _readOnlyPreviewPowers;
        public IReadOnlyDictionary<ushort, BookSnapshot> Books => _readOnlyBooks;

        public bool TryGetPreviewPower(uint picId, out ulong power)
        {
            return _previewPowers.TryGetValue(picId, out power);
        }

        public void ReplacePreviewPower(uint picId, ulong power)
        {
            _previewPowers[picId] = power;
        }

        public bool TryGetBook(ushort type, out BookSnapshot snapshot)
        {
            return _books.TryGetValue(type, out snapshot);
        }

        public void ReplaceBook(ushort type, List<GroupEntry> groups, List<PictureEntry> pictures, ulong picCombat)
        {
            _books[type] = new BookSnapshot(type, groups, pictures, picCombat);
        }

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
            _books.Clear();
            HasData = false;
        }
    }
}
