// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/DownDropItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class DownDropItemBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _group;
        public TextMeshProUGUI _lb_content;
        public Image _img_split;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_group), _group);
            EnsureBound(nameof(_lb_content), _lb_content);
            EnsureBound(nameof(_img_split), _img_split);
        }
    }
}
