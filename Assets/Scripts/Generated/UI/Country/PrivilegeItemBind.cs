// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/country/PrivilegeItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Country
{
    public partial class PrivilegeItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI nameLb;
        public TextMeshProUGUI disLb;
        public TextMeshProUGUI needLb;
        public RectTransform useBtn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(nameLb), nameLb);
            EnsureBound(nameof(disLb), disLb);
            EnsureBound(nameof(needLb), needLb);
            EnsureBound(nameof(useBtn), useBtn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
        }
    }
}
