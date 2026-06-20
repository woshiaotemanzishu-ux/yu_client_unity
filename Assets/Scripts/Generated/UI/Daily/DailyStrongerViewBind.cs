// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/daily/DailyStrongerView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Daily
{
    public partial class DailyStrongerViewBind : BaseView
    {
        public ScrollRect _item_scroller;
        public RectTransform Content;
        public ScrollRect _Scroller1;
        public RectTransform Content1;
        public Image _Image1;
        public Image _Image2;
        public GameObject _tpl_DailyStrongerItem;
        public GameObject _tpl_DailyStrongerTab;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_item_scroller), _item_scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_tpl_DailyStrongerItem), _tpl_DailyStrongerItem);
            EnsureBound(nameof(_tpl_DailyStrongerTab), _tpl_DailyStrongerTab);
        }
    }
}
