namespace Shenxiao.Module.Core.Mask
{
    /// <summary>51101 面具状态快照；不驱动角色外观、资源、特效或 UI。</summary>
    public sealed class MaskModel
    {
        public static readonly MaskModel Instance = new MaskModel(); private MaskModel() { }
        public byte MaskId { get; private set; } public uint EndTime { get; private set; } public bool HasData { get; private set; }
        public void ReplaceData(byte maskId, uint endTime) { MaskId = maskId; EndTime = endTime; HasData = true; }
        public void Reset() { MaskId = 0; EndTime = 0; HasData = false; }
    }
}
