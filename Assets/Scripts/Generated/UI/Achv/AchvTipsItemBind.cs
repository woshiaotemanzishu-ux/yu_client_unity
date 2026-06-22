// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/achv/AchvTipsItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Achv
{
    public partial class AchvTipsItemBind : BaseView
    {
        public Image bg;
        public Image _img_name_bg;
        public TextMeshProUGUI nameLb;
        public TextMeshProUGUI desLb;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public RectTransform goBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_img_name_bg), _img_name_bg);
            EnsureBound(nameof(nameLb), nameLb);
            EnsureBound(nameof(desLb), desLb);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(goBtn), goBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
