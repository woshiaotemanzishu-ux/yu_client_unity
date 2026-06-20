// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/daily/DailyLimitActivityView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Daily
{
    public partial class DailyLimitActivityViewBind : BaseView
    {
        public ScrollRect _list_item_con;
        public RectTransform _box_bottom;
        public GameObject _tpl_DailyLimitActivityItem;
        public GameObject _tpl_DailyBottomView;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_list_item_con), _list_item_con);
            EnsureBound(nameof(_box_bottom), _box_bottom);
            EnsureBound(nameof(_tpl_DailyLimitActivityItem), _tpl_DailyLimitActivityItem);
            EnsureBound(nameof(_tpl_DailyBottomView), _tpl_DailyBottomView);
        }
    }
}
