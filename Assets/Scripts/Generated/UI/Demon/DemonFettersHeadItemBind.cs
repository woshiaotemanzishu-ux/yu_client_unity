// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonFettersHeadItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonFettersHeadItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_head;
        public Image _img_head_mask;
        public Image _img_tips;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_head), _img_head);
            EnsureBound(nameof(_img_head_mask), _img_head_mask);
            EnsureBound(nameof(_img_tips), _img_tips);
        }
    }
}
