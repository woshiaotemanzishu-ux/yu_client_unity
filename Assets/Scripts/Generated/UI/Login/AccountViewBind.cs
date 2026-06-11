// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/AccountView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class AccountViewBind : BaseView
    {
        public RectTransform _gp_con;
        public TextMeshProUGUI _lb_server;
        public ScrollRect _scroller_address;
        public Image m_account;
        public Image m_enter_btn;
        public GameObject _tpl_AccountItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_lb_server), _lb_server);
            EnsureBound(nameof(_scroller_address), _scroller_address);
            EnsureBound(nameof(m_account), m_account);
            EnsureBound(nameof(m_enter_btn), m_enter_btn);
            EnsureBound(nameof(_tpl_AccountItem), _tpl_AccountItem);
        }
    }
}
