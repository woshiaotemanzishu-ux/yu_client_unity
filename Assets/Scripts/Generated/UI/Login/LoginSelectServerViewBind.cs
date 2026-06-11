// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginSelectServerView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginSelectServerViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_bg3;
        public Image _img_close;
        public ScrollRect _list_location;
        public ScrollRect _list_item;
        public Image _select_left;
        public ScrollRect _list_tab;
        public GameObject _tpl_LoginSelectServerItem;
        public GameObject _tpl_LoginSelectServerLocationItem;
        public GameObject _tpl_LoginSelectServerTabItem;
        public GameObject _tpl_EyouThAdultItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_list_location), _list_location);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_select_left), _select_left);
            EnsureBound(nameof(_list_tab), _list_tab);
            EnsureBound(nameof(_tpl_LoginSelectServerItem), _tpl_LoginSelectServerItem);
            EnsureBound(nameof(_tpl_LoginSelectServerLocationItem), _tpl_LoginSelectServerLocationItem);
            EnsureBound(nameof(_tpl_LoginSelectServerTabItem), _tpl_LoginSelectServerTabItem);
            EnsureBound(nameof(_tpl_EyouThAdultItem), _tpl_EyouThAdultItem);
        }
    }
}
