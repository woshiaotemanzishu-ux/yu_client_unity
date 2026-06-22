// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/activity/DailySupplyItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Activity
{
    public partial class DailySupplyItemBind : BaseView
    {
        public RectTransform _Group1;
        public Image bg;
        public Image _Image1;
        public TextMeshProUGUI title;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public RectTransform getBtn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public Image recImg;
        public Image reddot;
        public Image UnfinishImg;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(getBtn), getBtn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(recImg), recImg);
            EnsureBound(nameof(reddot), reddot);
            EnsureBound(nameof(UnfinishImg), UnfinishImg);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
