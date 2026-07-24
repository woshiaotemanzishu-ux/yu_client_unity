// MainUI HudSecondary 拆分后的手工 Bind；布局与字段由 HudAuxiliaryCreator 维护。
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIOnHookViewBind : BaseView
    {
        public RectTransform _box_outline_exp;
        public Image _img_outline_exp_bg1;
        public Image _img_outline_exp_bg;
        public RectTransform exp_show;
        public TextMeshProUGUI _lb_outline_exp;
        public RectTransform add_btn;
        public RectTransform add;
        public Image _img_add;
        public RectTransform _box_exp_btn;
        public Image exp_btn;
        public RectTransform _box_old_outline_exp;
        public Image _img_old_outline_exp_bg;
        public TextMeshProUGUI _lb_old_outline_exp;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_outline_exp), _box_outline_exp);
            EnsureBound(nameof(_img_outline_exp_bg1), _img_outline_exp_bg1);
            EnsureBound(nameof(_img_outline_exp_bg), _img_outline_exp_bg);
            EnsureBound(nameof(exp_show), exp_show);
            EnsureBound(nameof(_lb_outline_exp), _lb_outline_exp);
            EnsureBound(nameof(add_btn), add_btn);
            EnsureBound(nameof(add), add);
            EnsureBound(nameof(_img_add), _img_add);
            EnsureBound(nameof(_box_exp_btn), _box_exp_btn);
            EnsureBound(nameof(exp_btn), exp_btn);
            EnsureBound(nameof(_box_old_outline_exp), _box_old_outline_exp);
            EnsureBound(nameof(_img_old_outline_exp_bg), _img_old_outline_exp_bg);
            EnsureBound(nameof(_lb_old_outline_exp), _lb_old_outline_exp);
        }
    }
}
