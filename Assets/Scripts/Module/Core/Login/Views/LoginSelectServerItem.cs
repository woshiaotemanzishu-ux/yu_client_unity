using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选区服项(对标老客户端 login/LoginSelectServerItem.ts):服名(_lb_server_name)+ 模式图标 + 推荐/火爆标 +
    /// 已有角色的职业图标/等级。点击进入该服。
    ///
    /// SetData(serverName, level):服名 + 角色等级(>0 才显职业图标/等级)。降级:模式/职业/推荐标图标(login 图集)
    /// 待接 → 推荐标隐藏、文字即时。列表项,由选服界面克隆。
    /// </summary>
    public sealed class LoginSelectServerItem : LoginSelectServerItemBind
    {
        protected override void OnInit()
        {
            if (_img_tips != null) _img_tips.gameObject.SetActive(false); // 推荐/火爆标待 config
        }

        public void SetData(string serverName, int level)
        {
            if (_lb_server_name != null) _lb_server_name.text = serverName ?? "";
            bool hasRole = level > 0;
            if (_lb_level != null)
            {
                _lb_level.gameObject.SetActive(hasRole);
                if (hasRole) _lb_level.text = "Lv." + level;
            }
            if (_img_career != null) _img_career.gameObject.SetActive(hasRole);
        }
    }
}
