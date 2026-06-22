// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/competelist/CompetelistIntegralItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Competelist
{
    public partial class CompetelistIntegralItemBind : BaseView
    {
        public Image bgImg;
        public Image _img;
        public TextMeshProUGUI _lb_integral;
        public Image _red_dot;
        public Image _img_get;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bgImg), bgImg);
            EnsureBound(nameof(_img), _img);
            EnsureBound(nameof(_lb_integral), _lb_integral);
            EnsureBound(nameof(_red_dot), _red_dot);
            EnsureBound(nameof(_img_get), _img_get);
        }
    }
}
