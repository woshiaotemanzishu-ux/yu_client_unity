using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友申请条目(对标老客户端 friend/FriendApllyPopItem.ts):头像 + 昵称 + 等级 + 战力 + 通过/拒绝按钮
    /// (单条走 14005,response_type 0拒绝/1接受)。由 <see cref="FriendApllyPopView"/> 克隆
    /// <see cref="FriendApllyPopViewBind._tpl_FriendApllyPopItem"/> 铺列表。
    /// </summary>
    public sealed class FriendApllyPopItem : FriendApllyPopItemBind
    {
        private FriendModel.ApplyVo _vo;
        private bool _inited;

        protected override void OnInit() => EnsureInit();

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            UIUtil.AddClick(btn_no, () => OnClick(0));
            UIUtil.AddClick(btn_yes, () => OnClick(1));
        }

        public void SetData(FriendModel.ApplyVo vo)
        {
            EnsureInit();
            _vo = vo;
            if (vo == null) return;

            if (_lb_name != null) _lb_name.text = vo.Name;
            if (_lb_level != null) _lb_level.text = vo.Lv > 370 ? "神创" + (vo.Lv - 370) : "LV." + vo.Lv;
            if (_lb_fight != null) _lb_fight.text = vo.Combat.ToString();
            if (_vip_icon != null) _vip_icon.gameObject.SetActive(false); // 申请条目字段无 vip(对标 14006 无sex/vip)

            _ = LoadHead(vo);
        }

        private async System.Threading.Tasks.Task LoadHead(FriendModel.ApplyVo vo)
        {
            CustomHeadItem item = await FriendUiUtil.EnsureHead(head);
            if (item == null || _vo != vo) return;
            item.SetRoleData(vo.Career, 0, vo.Lv, showLevel: false);
        }

        private void OnClick(int responseType)
        {
            if (_vo == null) return;
            FriendController.Instance.OneApply(_vo.RoleId, responseType);
        }
    }
}
