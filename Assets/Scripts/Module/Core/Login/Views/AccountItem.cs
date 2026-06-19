using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 账号调试输入项(对标老客户端 login/AccountView.ts 内 AccountItem):描述(_lb_desc)+ 输入框(_ti_value)。
    ///
    /// SetDesc(desc) / SetValue(text) / GetValue()。输入框(TMP_InputField)自包含 → 完整可用。
    /// 由 AccountView 克隆(每个连接字段一行:账号/IP/端口/PID 等)。
    /// </summary>
    public sealed class AccountItem : AccountItemBind
    {
        /// <summary>填字段描述(对标 _lb_desc = data.value.desc)。</summary>
        public void SetDesc(string desc)
        {
            if (_lb_desc != null) _lb_desc.text = desc ?? "";
        }

        /// <summary>设输入值(对标 SetTextLabel)。</summary>
        public void SetValue(string value)
        {
            if (_ti_value != null) _ti_value.text = value ?? "";
        }

        /// <summary>取输入值(对标 GetTextLabel)。</summary>
        public string GetValue()
        {
            return _ti_value != null ? _ti_value.text : "";
        }
    }
}
