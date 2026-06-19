using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选区服页签项(对标老客户端 login/LoginSelectServerTabItem.ts):页签名(_lb_name)+ 选中底图(_img_bg)。
    ///
    /// SetData(name) + SetSelected(bool)。降级:选中底图 skin(login ui_Login_21)待图标 → 选中时显 _img_bg + 文字变色。
    /// 列表项,由选服界面克隆。
    /// </summary>
    public sealed class LoginSelectServerTabItem : LoginSelectServerTabItemBind
    {
        public void SetData(string name)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
        }

        public void SetSelected(bool selected)
        {
            if (_img_bg != null) _img_bg.gameObject.SetActive(selected); // 选中底图(skin 待图标)
            if (_lb_name != null) _lb_name.color = selected ? new Color(1f, 0.933f, 0.894f) : new Color(0.506f, 0.271f, 0.169f);
        }
    }
}
