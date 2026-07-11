using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Module.Core.Chat;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友列表项(对标老客户端 friend/FriendListItem.ts):头像 + 姓名 + 在线/离线(离线带时长) + 战力 + 亲密度 +
    /// VIP角标 + 私聊按钮(chatBtn,开 FriendChatView)+ 私聊未读红点(chatNumLabel/redDotGroup,数据源
    /// ChatModel.GetPrivateUnread,与好友页/私聊Tab共享同一份未读计数)+ 整行点击(touchGroup,开右键菜单,
    /// 自己不弹)。由 <see cref="FriendView"/> 克隆 <see cref="FriendViewBind._tpl_FriendListItem"/> 铺列表,
    /// 走 EnsureInit+SetData(不经 Show,对标 BagItemRenderer 套路)。
    ///
    /// 降级:VIP 角标图集/头像框图标未接真实素材(CustomHeadItem 本身已对标,仅默认头像;SVIP/VIP 数字图标 TODO)。
    /// </summary>
    public sealed class FriendListItem : FriendListItemBind
    {
        private FriendModel.FriendVo _vo;
        private bool _inited;

        protected override void OnInit() => EnsureInit();

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            UIUtil.AddClick(chatBtn, OnClickChat);
            UIUtil.AddClick(touchGroup, OnClickMenu);
            if (redDotGroup != null) redDotGroup.gameObject.SetActive(false);
        }

        public void SetData(FriendModel.FriendVo vo)
        {
            EnsureInit();
            _vo = vo;
            if (vo == null) return;

            if (lb_name != null) lb_name.text = vo.Name;
            if (lb_online != null)
            {
                if (vo.OnlineFlag == 1)
                {
                    lb_online.text = "(在线)";
                    lb_online.color = new Color(0.039f, 0.584f, 0.243f);
                }
                else
                {
                    lb_online.text = "(离线 " + FriendUiUtil.FormatOfflineDuration(vo.OfflineTime) + "前)";
                    lb_online.color = new Color(1f, 0.31f, 0.31f);
                }
            }
            if (lb_fight != null) lb_fight.text = vo.Combat.ToString();
            if (_lb_intimacy != null) _lb_intimacy.text = vo.Intimacy.ToString();
            if (_vip_icon != null) _vip_icon.gameObject.SetActive(vo.Vip > 0);

            RefreshChatRedDot();
            _ = LoadHead(vo);
        }

        /// <summary>私聊红点(对标老端 GetchatNum,数据源与私聊Tab红点共享)。由父 View 在 EVT_CHAT_PRIVATE_UPDATE 时重调。</summary>
        public void RefreshChatRedDot()
        {
            if (_vo == null || redDotGroup == null) return;
            int num = ChatModel.Instance.GetPrivateUnread(_vo.RoleId);
            redDotGroup.gameObject.SetActive(num > 0);
            if (num > 0 && chatNumLabel != null) chatNumLabel.text = num.ToString();
        }

        private async System.Threading.Tasks.Task LoadHead(FriendModel.FriendVo vo)
        {
            CustomHeadItem item = await FriendUiUtil.EnsureHead(head);
            if (item == null || _vo != vo) return;
            item.SetRoleData(vo.Career, vo.Turn, vo.Lv, showLevel: true);
        }

        private void OnClickChat()
        {
            if (_vo == null) return;
            FriendFlow.OpenChat(_vo.RoleId, _vo.Name, _vo.Career, _vo.Turn, _vo.Lv);
        }

        private void OnClickMenu()
        {
            if (_vo == null) return;
            if (RoleModel.Instance.RoleId == _vo.RoleId) return; // 自己不弹菜单(对标老端 touchGroup 拦截)
            FriendFlow.OpenMenu(_vo, (Vector2)Input.mousePosition);
        }
    }
}
