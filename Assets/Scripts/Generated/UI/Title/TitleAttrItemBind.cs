// 由 LayaUI 转换器生成结构后维护；来源: cdn/resource/game/title/TitleAttrItem.json。
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Title
{
    public partial class TitleAttrItemBind : BaseView
    {
        public RectTransform next_gp;
        public Image _Image1;
        public TextMeshProUGUI next_attr_lb;
        public TextMeshProUGUI now_attr_lb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(next_gp), next_gp);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(next_attr_lb), next_attr_lb);
            EnsureBound(nameof(now_attr_lb), now_attr_lb);
        }
    }
}
