using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 黑名单条目(对标老客户端 friend/FriendBlackListItm.ts):头像 + 昵称 + 在线/离线 + 战力 +
    /// "移出黑名单"按钮(btn_no,14007 type=3 取消拉黑)+ 整行点击(touchGroup,开右键菜单,自己不弹)。
    /// 由 <see cref="FriendBlackListPopView"/> 克隆 <see cref="FriendBlackListPopViewBind._tpl_FriendBlackListItm"/> 铺列表。
    /// </summary>
    public sealed class FriendBlackListItm : FriendBlackListItmBind
    {
        private FriendModel.FriendVo _vo;
        private bool _inited;

        protected override void OnInit() => EnsureInit();

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            UIUtil.AddClick(btn_no, OnClickRemove);
            UIUtil.AddClick(touchGroup, OnClickMenu);
        }

        public void SetData(FriendModel.FriendVo vo)
        {
            EnsureInit();
            _vo = vo;
            if (vo == null) return;

            if (lb_online != null)
            {
                if (vo.OnlineFlag == 1)
                {
                    lb_online.text = "(在线)";
                    lb_online.color = new Color(0.039f, 0.584f, 0.243f);
                }
                else
                {
                    lb_online.text = "(离线)";
                    lb_online.color = new Color(1f, 0.31f, 0.31f);
                }
            }
            if (_lb_name != null) _lb_name.text = vo.Name;
            if (_lb_fight != null) _lb_fight.text = vo.Combat.ToString();

            _ = LoadHead(vo);
        }

        private async System.Threading.Tasks.Task LoadHead(FriendModel.FriendVo vo)
        {
            CustomHeadItem item = await FriendUiUtil.EnsureHead(head);
            if (item == null || _vo != vo) return;
            item.SetRoleData(vo.Career, vo.Turn, vo.Lv, showLevel: true);
        }

        private void OnClickRemove()
        {
            if (_vo == null) return;
            FriendController.Instance.FriendsOperate(3, _vo.RoleId); // type=3 取消拉黑
        }

        private void OnClickMenu()
        {
            if (_vo == null) return;
            if (RoleModel.Instance.RoleId == _vo.RoleId) return;
            FriendFlow.OpenMenu(_vo, (Vector2)Input.mousePosition);
        }
    }
}
