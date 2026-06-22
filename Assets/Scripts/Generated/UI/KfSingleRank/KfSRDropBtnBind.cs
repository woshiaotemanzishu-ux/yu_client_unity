// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfSingleRank/kfSRDropBtn.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfSingleRank
{
    public partial class KfSRDropBtnBind : BaseView
    {
        public RectTransform _group;
        public Image _Image1;
        public Image _img_arrow;
        public TextMeshProUGUI _lb_content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_group), _group);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_arrow), _img_arrow);
            EnsureBound(nameof(_lb_content), _lb_content);
        }
    }
}
