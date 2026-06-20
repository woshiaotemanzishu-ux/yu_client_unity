// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/MaterialsItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class MaterialsItemBind : BaseView
    {
        public Image _Image11;
        public Image tip;
        public TextMeshProUGUI targetName;
        public Image btn;
        public TextMeshProUGUI labelDisplay;
        public Image _Image2;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(tip), tip);
            EnsureBound(nameof(targetName), targetName);
            EnsureBound(nameof(btn), btn);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_Image2), _Image2);
        }
    }
}
