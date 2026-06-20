// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/CompositeGetMatView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class CompositeGetMatViewBind : BaseView
    {
        public Image _bg1;
        public Image _bg2;
        public Image _Image2;
        public TextMeshProUGUI _Image3;
        public Image btnClose;
        public ScrollRect itemScroller;
        public RectTransform item_con;
        public GameObject _tpl_MaterialsItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg1), _bg1);
            EnsureBound(nameof(_bg2), _bg2);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(itemScroller), itemScroller);
            EnsureBound(nameof(item_con), item_con);
            EnsureBound(nameof(_tpl_MaterialsItem), _tpl_MaterialsItem);
        }
    }
}
