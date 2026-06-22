// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/activityOverView/ActivityOverView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.ActivityOverView
{
    public partial class ActivityOverViewBind : BaseView
    {
        public RectTransform _rew_box;
        public ScrollRect _item_list;
        public RectTransform _click_box;
        public Image _img_red;
        public GameObject _tpl_ActivityOverListItemView;
        public GameObject _tpl_ActivityOverItem;
        public GameObject _tpl_ActivityOverRewardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_rew_box), _rew_box);
            EnsureBound(nameof(_item_list), _item_list);
            EnsureBound(nameof(_click_box), _click_box);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_tpl_ActivityOverListItemView), _tpl_ActivityOverListItemView);
            EnsureBound(nameof(_tpl_ActivityOverItem), _tpl_ActivityOverItem);
            EnsureBound(nameof(_tpl_ActivityOverRewardItem), _tpl_ActivityOverRewardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
