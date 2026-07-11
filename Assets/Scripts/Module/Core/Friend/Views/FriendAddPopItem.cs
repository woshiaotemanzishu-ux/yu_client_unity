using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 加好友搜索/推荐结果条目(对标老客户端 friend/FriendAddPopItem.ts):头像 + 昵称 + 等级(&gt;370 显"神创N")+
    /// 战力 + VIP角标 + 添加按钮(addBtn/haveAdd 互斥显隐)。点添加:自己拦截 + 假人(IsFaker)强制 role_id=0 仍照发
    /// 14003(对标老端"可疑边界行为",原样保留)。由 <see cref="FriendAddPopView"/> 克隆
    /// <see cref="FriendAddPopViewBind._tpl_FriendAddPopItem"/> 铺列表。
    /// </summary>
    public sealed class FriendAddPopItem : FriendAddPopItemBind
    {
        private FriendModel.RecommendVo _vo;
        private bool _inited;

        protected override void OnInit() => EnsureInit();

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            UIUtil.AddClick(addBtn, OnClickAdd);
        }

        public void SetData(FriendModel.RecommendVo vo)
        {
            EnsureInit();
            _vo = vo;
            if (vo == null) return;

            if (_lb_name != null) _lb_name.text = vo.Name;
            if (_lb_level != null) _lb_level.text = vo.Lv > 370 ? "神创" + (vo.Lv - 370) : "LV." + vo.Lv;
            if (_lb_fight != null) _lb_fight.text = vo.Combat.ToString();
            if (_vip_icon != null) _vip_icon.gameObject.SetActive(vo.Vip > 0);
            if (haveAdd != null) haveAdd.gameObject.SetActive(vo.ApplyFriend);
            if (addBtn != null) addBtn.gameObject.SetActive(!vo.ApplyFriend);

            _ = LoadHead(vo);
        }

        private async System.Threading.Tasks.Task LoadHead(FriendModel.RecommendVo vo)
        {
            CustomHeadItem item = await FriendUiUtil.EnsureHead(head);
            if (item == null || _vo != vo) return;
            item.SetRoleData(vo.Career, vo.Turn, vo.Lv, showLevel: false);
        }

        private void OnClickAdd()
        {
            if (_vo == null || _vo.RoleId == 0) return;
            if (_vo.RoleId == RoleModel.Instance.RoleId)
            {
                TipsManager.Toast("无法添加自己为好友");
                return;
            }
            long roleId = _vo.RoleId;
            if (_vo.IsFaker)
            {
                // 对标老端 FriendAddPopItem.addClick:假人位强制 role_id=0 仍照发,且本地立即切"已申请"态。
                roleId = 0;
                _vo.ApplyFriend = true;
                if (haveAdd != null) haveAdd.gameObject.SetActive(true);
                if (addBtn != null) addBtn.gameObject.SetActive(false);
            }
            FriendController.Instance.AddFriendApply(roleId);
        }
    }
}
