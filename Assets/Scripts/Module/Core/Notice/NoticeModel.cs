using System.Collections.Generic;

namespace Shenxiao.Module.Core.Notice
{
    /// <summary>
    /// 公告/广播数据（对标老客户端 SysInfo/Chuanwen 的数据侧）：
    /// - 系统公告(11020)：最近一条文本（跑马灯/系统提示用）。
    /// - 传闻 chuanwen(11015/11018)：服务器全服广播，保留最近若干条供滚动展示。
    /// 只存数据；显示（跑马灯/弹层）由后续 UI（待用户验收）订阅事件读这里。
    /// </summary>
    public sealed class NoticeModel
    {
        public static readonly NoticeModel Instance = new NoticeModel();
        private NoticeModel() { }

        private const int ChuanwenMax = 50;

        /// <summary>最近一条系统公告(11020)文本。</summary>
        public string LastSysNotice = "";

        /// <summary>最近的传闻广播（含可能的发送者名，11015/11018），最多 50 条，越新越靠后。</summary>
        public readonly List<ChuanwenItem> RecentChuanwen = new List<ChuanwenItem>();

        public void SetSysNotice(string content)
        {
            LastSysNotice = content ?? "";
        }

        public void PushChuanwen(int moduleId, int id, string content, long senderId = 0)
        {
            RecentChuanwen.Add(new ChuanwenItem(moduleId, id, content ?? "", senderId));
            if (RecentChuanwen.Count > ChuanwenMax)
            {
                RecentChuanwen.RemoveAt(0);
            }
        }

        public void Clear()
        {
            LastSysNotice = "";
            RecentChuanwen.Clear();
        }

        public readonly struct ChuanwenItem
        {
            public readonly int ModuleId;
            public readonly int Id;
            public readonly string Content;
            public readonly long SenderId; // 11018 带发送者；11015 为 0

            public ChuanwenItem(int moduleId, int id, string content, long senderId)
            {
                ModuleId = moduleId;
                Id = id;
                Content = content;
                SenderId = senderId;
            }
        }
    }
}
