using System.Collections.Generic;
namespace Shenxiao.Module.Core.Designation
{
    /// <summary>41101 称号列表快照；保留服务端顺序，不作过期或配置过滤。</summary>
    public sealed class DesignationModel
    {
        public sealed class Entry { public uint Id { get; } public byte Order { get; } public uint EndTime { get; } public Entry(uint id, byte order, uint endTime) { Id = id; Order = order; EndTime = endTime; } }
        public static readonly DesignationModel Instance = new DesignationModel(); private readonly List<Entry> _entries = new List<Entry>(); private DesignationModel() { }
        public uint CurrentUsedId { get; private set; } public IReadOnlyList<Entry> Entries => _entries; public bool HasData { get; private set; }
        public void ReplaceData(uint currentUsedId, List<Entry> entries) { CurrentUsedId = currentUsedId; _entries.Clear(); if (entries != null) _entries.AddRange(entries); HasData = true; }
        public void Reset() { CurrentUsedId = 0; _entries.Clear(); HasData = false; }
    }
}
