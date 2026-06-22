// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eternity/EternityMonItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eternity
{
    public partial class EternityMonItemBind : BaseView
    {
        public RectTransform click;
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_lv;
        public Image _img_select;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click), click);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_lv), _lb_lv);
            EnsureBound(nameof(_img_select), _img_select);
        }
    }
}
