// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginNoticeView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginNoticeViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title;
        public TextMeshProUGUI _lb_test_server;
        public RectTransform _btn_read;
        public Image _Image122;
        public ScrollRect _scroll_con;
        public RectTransform _gp_item;
        public RectTransform _gp_test_server;
        public Image _Image1;
        public TextMeshProUGUI _lb_server_time;
        public TextMeshProUGUI _lb_server_name;
        public ScrollRect _list_tab;
        public GameObject _tpl_LoginNoticeBtnItem;
        public GameObject _tpl_LoginNoticeItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_test_server), _lb_test_server);
            EnsureBound(nameof(_btn_read), _btn_read);
            EnsureBound(nameof(_Image122), _Image122);
            EnsureBound(nameof(_scroll_con), _scroll_con);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_gp_test_server), _gp_test_server);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_server_time), _lb_server_time);
            EnsureBound(nameof(_lb_server_name), _lb_server_name);
            EnsureBound(nameof(_list_tab), _list_tab);
            EnsureBound(nameof(_tpl_LoginNoticeBtnItem), _tpl_LoginNoticeBtnItem);
            EnsureBound(nameof(_tpl_LoginNoticeItem), _tpl_LoginNoticeItem);
        }
    }
}
