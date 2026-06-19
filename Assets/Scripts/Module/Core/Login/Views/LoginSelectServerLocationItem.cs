using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选区服地区项(对标老客户端 login/LoginSelectServerLocationItem.ts):地区名(_lb_name)+ 选中底图(_img_bg)。
    ///
    /// SetData(name) + SetSelected(bool)。降级:底图 skin(login ui_Login_btn_1/2)待图标 → 选中用文字色区分。
    /// 列表项,由选服界面克隆。
    /// </summary>
    public sealed class LoginSelectServerLocationItem : LoginSelectServerLocationItemBind
    {
        public void SetData(string name)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
        }

        public void SetSelected(bool selected)
        {
            // 底图 skin(ui_Login_btn_1/2)待图标;先用文字色区分
            if (_lb_name != null) _lb_name.color = selected ? new Color(0.682f, 0.384f, 0.192f) : new Color(0.894f, 0.898f, 0.973f);
        }
    }
}
