// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/advertising/AdvertisingMainView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Advertising
{
    public partial class AdvertisingMainViewBind : BaseView
    {
        public Image _img_title;
        public RectTransform _box_time;
        public ScrollRect _list_item;
        public GameObject _tpl_AdvertisingItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_box_time), _box_time);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_tpl_AdvertisingItem), _tpl_AdvertisingItem);
        }
    }
}
