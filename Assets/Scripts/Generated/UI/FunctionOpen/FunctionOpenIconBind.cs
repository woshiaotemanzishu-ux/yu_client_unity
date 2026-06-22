// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/functionOpen/FunctionOpenIcon.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FunctionOpen
{
    public partial class FunctionOpenIconBind : BaseView
    {
        public RectTransform click_box;
        public Image bg;
        public TextMeshProUGUI tips;
        public RectTransform icon_con;
        public Image icon;
        public RectTransform _box_model;
        public Image red_dot;
        public Image _img_model_txt;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click_box), click_box);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(icon_con), icon_con);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(_box_model), _box_model);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(_img_model_txt), _img_model_txt);
        }
    }
}
