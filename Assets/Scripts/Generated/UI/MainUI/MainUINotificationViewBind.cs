// MainUI HudSecondary 拆分后的手工 Bind；布局与字段由 HudAuxiliaryCreator 维护。
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUINotificationViewBind : BaseView
    {
        public RectTransform _box_help;
        public Image _img_help;
        public RectTransform _box_help_tips;
        public Image _img_help_tips;
        public TextMeshProUGUI _lb_help;
        public RectTransform _box_notification_bar;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_help), _box_help);
            EnsureBound(nameof(_img_help), _img_help);
            EnsureBound(nameof(_box_help_tips), _box_help_tips);
            EnsureBound(nameof(_img_help_tips), _img_help_tips);
            EnsureBound(nameof(_lb_help), _lb_help);
            EnsureBound(nameof(_box_notification_bar), _box_notification_bar);
        }
    }
}
