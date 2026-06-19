// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/map/WorldMapView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Map
{
    public partial class WorldMapViewBind : BaseView
    {
        public ScrollRect scroll;
        public RectTransform map;
        public Image map_bg;
        public RectTransform scene_con;
        public Image frame_bg;
        public Image map_cloud;
        public GameObject _tpl_WorldMapItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(map), map);
            EnsureBound(nameof(map_bg), map_bg);
            EnsureBound(nameof(scene_con), scene_con);
            EnsureBound(nameof(frame_bg), frame_bg);
            EnsureBound(nameof(map_cloud), map_cloud);
            EnsureBound(nameof(_tpl_WorldMapItem), _tpl_WorldMapItem);
        }
    }
}
