// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bag/SelectGiftItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bag
{
    public partial class SelectGiftItemBind : BaseView
    {
        public RectTransform _Group1;
        public Image bg;
        public Image _bg_title;
        public RectTransform item_group;
        public Image num_bg;
        public TextMeshProUGUI name_label;
        public TextMeshProUGUI num_label;
        public RectTransform reduce_btn;
        public Image _Image111;
        public RectTransform increase_btn;
        public Image _Image11;
        public RectTransform max_btn;
        public Image _Image1;
        public TextMeshProUGUI haveLb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_bg_title), _bg_title);
            EnsureBound(nameof(item_group), item_group);
            EnsureBound(nameof(num_bg), num_bg);
            EnsureBound(nameof(name_label), name_label);
            EnsureBound(nameof(num_label), num_label);
            EnsureBound(nameof(reduce_btn), reduce_btn);
            EnsureBound(nameof(_Image111), _Image111);
            EnsureBound(nameof(increase_btn), increase_btn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(max_btn), max_btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(haveLb), haveLb);
        }
    }
}
