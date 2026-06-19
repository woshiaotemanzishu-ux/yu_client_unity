// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/WithBtnHSlider.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class WithBtnHSliderBind : BaseView
    {
        public RectTransform slider_con;
        public Image reduce_btn;
        public Image increase_btn;
        public GameObject _tpl_GreenHSlider;

        protected override void BindNodes()
        {
            EnsureBound(nameof(slider_con), slider_con);
            EnsureBound(nameof(reduce_btn), reduce_btn);
            EnsureBound(nameof(increase_btn), increase_btn);
            EnsureBound(nameof(_tpl_GreenHSlider), _tpl_GreenHSlider);
        }
    }
}
