// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/map/AreaMapView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Map
{
    public partial class AreaMapViewBind : BaseView
    {
        public Image bg;
        public ScrollRect map_scroll;
        public RectTransform scroll_group;
        public Image map_img;
        public RectTransform item_con;
        public RectTransform way_point_con;
        public Image main_role_point;
        public TextMeshProUGUI role_name;
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _Label1;
        public RectTransform way_con;
        public ScrollRect mon_scroll;
        public ScrollRect mon_scroll_group;
        public GameObject _tpl_AreaMapMonItem;
        public GameObject _tpl_AreaMapPonitItem;
        public GameObject _tpl_AreaMapWayPonitItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(map_scroll), map_scroll);
            EnsureBound(nameof(scroll_group), scroll_group);
            EnsureBound(nameof(map_img), map_img);
            EnsureBound(nameof(item_con), item_con);
            EnsureBound(nameof(way_point_con), way_point_con);
            EnsureBound(nameof(main_role_point), main_role_point);
            EnsureBound(nameof(role_name), role_name);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(way_con), way_con);
            EnsureBound(nameof(mon_scroll), mon_scroll);
            EnsureBound(nameof(mon_scroll_group), mon_scroll_group);
            EnsureBound(nameof(_tpl_AreaMapMonItem), _tpl_AreaMapMonItem);
            EnsureBound(nameof(_tpl_AreaMapPonitItem), _tpl_AreaMapPonitItem);
            EnsureBound(nameof(_tpl_AreaMapWayPonitItem), _tpl_AreaMapWayPonitItem);
        }
    }
}
