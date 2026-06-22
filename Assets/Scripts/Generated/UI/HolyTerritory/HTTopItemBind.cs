// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyTerritory/HTTopItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyTerritory
{
    public partial class HTTopItemBind : BaseView
    {
        public RectTransform _Group1;
        public RectTransform dsgt;
        public Image _img_title;
        public RectTransform head;
        public Image _Image1;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI fight;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(dsgt), dsgt);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(head), head);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(nameLabel), nameLabel);
            EnsureBound(nameof(fight), fight);
        }
    }
}
