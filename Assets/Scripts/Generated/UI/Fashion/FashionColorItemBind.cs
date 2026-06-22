// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/fashion/FashionColorItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Fashion
{
    public partial class FashionColorItemBind : BaseView
    {
        public Image bg;
        public Image select;
        public Image red;
        public Image @lock;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(select), select);
            EnsureBound(nameof(red), red);
            EnsureBound(nameof(@lock), @lock);
        }
    }
}
