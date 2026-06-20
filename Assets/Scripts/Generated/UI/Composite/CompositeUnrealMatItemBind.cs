// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/CompositeUnrealMatItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class CompositeUnrealMatItemBind : BaseView
    {
        public RectTransform _item;
        public RectTransform _gp_con;
        public Image _Image1;
        public Image _img_add;
        public Image _img_lock;
        public RectTransform gp_count;
        public TextMeshProUGUI _count;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_item), _item);
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_add), _img_add);
            EnsureBound(nameof(_img_lock), _img_lock);
            EnsureBound(nameof(gp_count), gp_count);
            EnsureBound(nameof(_count), _count);
        }
    }
}
