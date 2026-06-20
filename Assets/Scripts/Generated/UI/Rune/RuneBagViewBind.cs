// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/rune/RuneBagView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Rune
{
    public partial class RuneBagViewBind : BaseView
    {
        public Image _Image11;
        public Image _Image2;
        public Image _Image3;
        public ScrollRect bag_scroll;
        public RectTransform none_conta;
        public RectTransform getBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public TextMeshProUGUI tips;
        public Image tips_icon;
        public Image closeBtn;
        public TextMeshProUGUI label1;
        public GameObject _tpl_RuneBagItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(bag_scroll), bag_scroll);
            EnsureBound(nameof(none_conta), none_conta);
            EnsureBound(nameof(getBtn), getBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(tips_icon), tips_icon);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(label1), label1);
            EnsureBound(nameof(_tpl_RuneBagItem), _tpl_RuneBagItem);
        }
    }
}
