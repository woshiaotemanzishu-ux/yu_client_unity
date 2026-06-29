using System.Collections.Generic;

namespace Shenxiao.Module.Core.Svip
{
    /// <summary>
    /// 至尊VIP(SVIP)数据(对标老客户端 SvipMainModel)。承载 45120 下发的 open_act_id + 特权内容列表,
    /// 供 SvipMainView 展示。open_act_id>0 表示活动开启(主界面显示 45120 图标)。
    /// </summary>
    public sealed class SvipMainModel
    {
        public static readonly SvipMainModel Instance = new SvipMainModel();
        private SvipMainModel() { }

        public sealed class ContentVo
        {
            public int Type;
            public readonly List<int> ContentIds = new List<int>();
        }

        public int OpenActId;
        public readonly List<ContentVo> ContentList = new List<ContentVo>();

        public void Reset()
        {
            OpenActId = 0;
            ContentList.Clear();
        }
    }
}
