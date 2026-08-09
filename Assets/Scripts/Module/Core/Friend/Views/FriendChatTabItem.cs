using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Module.Core.Chat;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 私聊会话 Tab 条目(对标老客户端 friend/FriendChatTabItem.ts):头像 + 昵称 + 未读红点(数据源
    /// ChatModel.GetPrivateUnread,与好友页/私聊行红点共享同一份计数)。点击 → 通知父窗切换当前会话。
    /// 由 <see cref="FriendChatView"/> 克隆 <see cref="FriendChatViewBind._tpl_FriendChatTabItem"/> 铺 Tab 列表。
    /// </summary>
    public sealed class FriendChatTabItem : FriendChatTabItemBind
    {
        private long _roleId;
        private System.Action<long> _onClick;
        private bool _inited;

        protected override void OnInit() => EnsureInit();

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            UIUtil.AddClick(click, () => _onClick?.Invoke(_roleId));
        }

        public void SetOnClick(System.Action<long> cb) => _onClick = cb;

        public void SetData(long roleId, bool isActive)
        {
            EnsureInit();
            _roleId = roleId;
            FriendModel.ChatPartnerInfo info = FriendModel.Instance.GetChatPartner(roleId);
            if (_lb_name != null) _lb_name.text = info != null ? info.Name : ("角色" + roleId);

            // 老端 FriendChatTabItem.dataChanged: index==0 使用 uilt_018，其他会话使用
            // uilt_019。Prefab 默认只保存了 uilt_019，之前忽略 isActive 会让当前会话无选中态。
            if (_Image1 != null)
            {
                _ = ResManager.SetImageAsync(
                    _Image1,
                    GameResPath.GetIcon("friend", isActive ? "uilt_018" : "uilt_019"),
                    nativeSize: false);
            }

            RefreshRedDot();
            _ = LoadHead(roleId, info);
        }

        public void RefreshRedDot()
        {
            int num = ChatModel.Instance.GetPrivateUnread(_roleId);
            if (_gp_redDot != null) _gp_redDot.gameObject.SetActive(num > 0);
            if (num > 0 && _lb_num != null) _lb_num.text = num.ToString();
        }

        private async System.Threading.Tasks.Task LoadHead(long roleId, FriendModel.ChatPartnerInfo info)
        {
            CustomHeadItem item = await FriendUiUtil.EnsureHead(head);
            if (item == null || _roleId != roleId) return;
            item.SetRoleData(info?.Career ?? 1, info?.Turn ?? 0, info?.Lv ?? 1, showLevel: false);
        }
    }
}
