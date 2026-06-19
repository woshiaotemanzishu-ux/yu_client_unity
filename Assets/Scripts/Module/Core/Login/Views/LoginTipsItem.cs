using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录提示项(对标老客户端 login/LoginTipsItem.ts):一行提示文本(_lb_desc)。SetData(content)。
    /// 由 LoginTipsView 克隆。自包含 → 可用。
    /// </summary>
    public sealed class LoginTipsItem : LoginTipsItemBind
    {
        public void SetData(string content)
        {
            if (_lb_desc != null) _lb_desc.text = content ?? "";
        }
    }
}
