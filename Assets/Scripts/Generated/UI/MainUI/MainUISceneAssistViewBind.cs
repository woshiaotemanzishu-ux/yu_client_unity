// MainUI HudSecondary 拆分后的手工 Bind；布局与字段由 HudAuxiliaryCreator 维护。
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUISceneAssistViewBind : BaseView
    {
        public RectTransform _box_auto_effect;
        public RectTransform _box_please;
        public Image _img_please;
        public RectTransform _gp_t_map;
        public RectTransform _gp_pro;
        public Image _img_rpr;
        public Image _img_tt_record;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_auto_effect), _box_auto_effect);
            EnsureBound(nameof(_box_please), _box_please);
            EnsureBound(nameof(_img_please), _img_please);
            EnsureBound(nameof(_gp_t_map), _gp_t_map);
            EnsureBound(nameof(_gp_pro), _gp_pro);
            EnsureBound(nameof(_img_rpr), _img_rpr);
            EnsureBound(nameof(_img_tt_record), _img_tt_record);
        }
    }
}
