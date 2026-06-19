// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainStronger/MainUIStrongerView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainStronger
{
    public partial class MainUIStrongerViewBind : BaseView
    {
        public RectTransform btnClose;
        public ScrollRect listStrongBtn;
        public ScrollRect _panel_ad;
        public Image _img_left;
        public Image _img_center;
        public Image _img_right;
        public ScrollRect _panel_toggle;
        public GameObject _tpl_MainUIStrongerBtn;

        protected override void BindNodes()
        {
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(listStrongBtn), listStrongBtn);
            EnsureBound(nameof(_panel_ad), _panel_ad);
            EnsureBound(nameof(_img_left), _img_left);
            EnsureBound(nameof(_img_center), _img_center);
            EnsureBound(nameof(_img_right), _img_right);
            EnsureBound(nameof(_panel_toggle), _panel_toggle);
            EnsureBound(nameof(_tpl_MainUIStrongerBtn), _tpl_MainUIStrongerBtn);
        }
    }
}
