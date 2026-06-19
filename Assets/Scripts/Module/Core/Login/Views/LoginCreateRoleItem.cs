using System;
using Shenxiao.Generated.UI.Login;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 建角色职业选择项(对标老客户端 login/LoginCreateRoleItem.ts):职业图标(_img_icon)+ 职业名(_lb_career),点击选职业。
    ///
    /// SetData(index, careerName, onClick) + SetSelected(bool)。降级:职业图标/选中底图 skin(login 图集 + vo.select_icon)
    /// 待接 → 名字/点击/选中色 即时可用;入场补间动画不做。列表项,由建角界面克隆。
    /// </summary>
    public sealed class LoginCreateRoleItem : LoginCreateRoleItemBind
    {
        private Action<int> _onClick;
        private int _index;

        protected override void OnInit()
        {
            BindClick(_box_head, () => { if (_onClick != null) _onClick(_index); });
        }

        public void SetData(int index, string careerName, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;
            if (_lb_career != null) _lb_career.text = careerName ?? "";
        }

        public void SetSelected(bool selected)
        {
            // 选中底图/职业图标 skin(ui_Login_02/03 + vo.select_icon)待 login 图集;先用文字色区分
            if (_lb_career != null) _lb_career.color = selected ? new Color(1f, 0.949f, 0.765f) : Color.white;
        }

        private void BindClick(Component area, Action onClick)
        {
            if (area == null) return;
            Image img = area.GetComponent<Image>();
            if (img == null) img = area.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
