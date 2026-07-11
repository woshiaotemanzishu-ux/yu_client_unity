using Shenxiao.Generated.UI.Friend;
using Shenxiao.Module.Core.Chat;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 私聊消息气泡·自己(对标老客户端 friend/FriendMineChatItem.ts,老端以继承 FriendChatItem 复用逻辑;
    /// 本端因 Bind 基类不同各自独立实现,字段/行为等价)。烤在 FriendChatView 自身 __Templates 内(非独立 prefab)→
    /// 头像走 Addressable 懒加载(<see cref="FriendUiUtil.EnsureHead"/>),与列表项一致。
    /// 由 <see cref="FriendChatView"/> 在发送/加载自己消息时动态 Instantiate 进 Content1。
    /// </summary>
    public sealed class FriendMineChatItem : FriendMineChatItemBind
    {
        private bool _inited;

        protected override void OnInit() => EnsureInit();

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            if (_vip_icon != null) _vip_icon.gameObject.SetActive(false);
        }

        public void SetData(ChatMessage msg)
        {
            EnsureInit();
            if (msg == null) return;

            string senderName = msg.Figure != null ? msg.Figure.name : "";
            if (nameLabel != null) nameLabel.text = " " + senderName + " ";
            if (_lb_time != null) _lb_time.text = FriendUiUtil.FormatChatTime(msg.Time);
            if (contentLabel != null) contentLabel.text = msg.Message;

            _ = LoadHead(msg);
        }

        private async System.Threading.Tasks.Task LoadHead(ChatMessage msg)
        {
            CustomHeadItem item = await FriendUiUtil.EnsureHead(head);
            if (item == null || msg?.Figure == null) return;
            item.SetRoleData(msg.Figure.career, msg.Figure.turn, msg.Figure.level, showLevel: false);
        }
    }
}
