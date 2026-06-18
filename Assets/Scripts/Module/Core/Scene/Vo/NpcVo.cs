using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Scene.Vo
{
    /// <summary>
    /// 场景 NPC 数据(对标服务端 pt_121 item_to_bin_0,12100/12103 列表项)。
    /// 字段:NpcId(i) IsShow(c) SceneId(i) X(h) Y(h) Args(s)。
    /// 任务图标(task_icon)由 12020 单独推送后回填。NPC 的图标/形象/类型从 config_npc 按 NpcId 查。
    /// </summary>
    public sealed class NpcVo
    {
        public int NpcId;
        public int IsShow;
        public int SceneId;
        public int X;
        public int Y;
        public string Args = "";

        /// <summary>12020 任务图标标记(0 无;>0 有任务),进场后由 SC_NPC_ICON_REFRESH 回填。</summary>
        public int TaskIcon;

        public void ReadFromProtocal(NetReader r)
        {
            NpcId = (int)r.ReadU32();
            IsShow = r.ReadU8();
            SceneId = (int)r.ReadU32();
            X = r.ReadU16();
            Y = r.ReadU16();
            Args = r.ReadString();
        }
    }
}
