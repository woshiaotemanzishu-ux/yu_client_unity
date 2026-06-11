// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginSelectRoleView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginSelectRoleViewBind : BaseView
    {
        public RectTransform _gp_model;
        public ScrollRect _panel_item;
        public Image _img_enter;
        public Image _img_return;
        public GameObject _tpl_LoginSelectRoleItem;
        public GameObject _tpl_EyouThAdultItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_model), _gp_model);
            EnsureBound(nameof(_panel_item), _panel_item);
            EnsureBound(nameof(_img_enter), _img_enter);
            EnsureBound(nameof(_img_return), _img_return);
            EnsureBound(nameof(_tpl_LoginSelectRoleItem), _tpl_LoginSelectRoleItem);
            EnsureBound(nameof(_tpl_EyouThAdultItem), _tpl_EyouThAdultItem);
        }
    }
}
