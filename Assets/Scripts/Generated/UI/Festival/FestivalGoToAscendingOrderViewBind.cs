// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/festival/FestivalGoToAscendingOrderView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Festival
{
    public partial class FestivalGoToAscendingOrderViewBind : BaseView
    {
        public Image bgImg;
        public RectTransform gotoBtn;
        public RectTransform iconBox;
        public RectTransform effectBox;
        public Image iconImg;
        public ScrollRect descList;
        public GameObject _tpl_FestivalInfoListItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bgImg), bgImg);
            EnsureBound(nameof(gotoBtn), gotoBtn);
            EnsureBound(nameof(iconBox), iconBox);
            EnsureBound(nameof(effectBox), effectBox);
            EnsureBound(nameof(iconImg), iconImg);
            EnsureBound(nameof(descList), descList);
            EnsureBound(nameof(_tpl_FestivalInfoListItem), _tpl_FestivalInfoListItem);
        }
    }
}
