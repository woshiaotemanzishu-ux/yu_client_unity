using Shenxiao.Generated.UI.Friend;
using Shenxiao.Module.Core.Chat;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 私聊消息气泡·对方(对标老客户端 friend/FriendChatItem.ts):头像 + 昵称 + 时间 + 文本内容。
    /// 独立 prefab(Assets/Prefabs/UI/Friend/FriendChatItem.prefab),自带局部 <see cref="_tpl_CustomHeadItem"/> 模板
    /// (不同于列表项走 Addressable 懒加载 common/CustomHeadItem——这份已烤在同 prefab,直接本地 Instantiate)。
    /// 由 <see cref="FriendChatView"/> 在收到/加载私聊消息时动态 Instantiate 进 Content1(对标老端 new FriendChatItem(parent))。
    ///
    /// 降级:VIP 角标/头像框皮肤(对标 dress_list Bubble/Photo 类型)未接 DressModel,聊天气泡走默认底图;
    /// 正文超链接(物品/位置分享)未解析,原样显示文本。
    /// </summary>
    public sealed class FriendChatItem : FriendChatItemBind
    {
        private CustomHeadItem _head;
        private bool _inited;

        protected override void OnInit() => EnsureInit();

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            if (_tpl_CustomHeadItem != null && head != null)
            {
                GameObject go = Instantiate(_tpl_CustomHeadItem, head);
                go.SetActive(true);
                _head = go.GetComponent<CustomHeadItem>();
                if (_head != null) { _head.Show(); _head.SetActiveFrame(false); }
            }
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

            if (_head != null && msg.Figure != null)
            {
                _head.SetRoleData(msg.Figure.career, msg.Figure.turn, msg.Figure.level, showLevel: false);
            }
        }
    }
}
