// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/map/WorldMapItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Map
{
    public partial class WorldMapItemBind : BaseView
    {
        public RectTransform city_map_group;
        public Image city_img;
        public RectTransform label_con;
        public Image _Image1;
        public TextMeshProUGUI city_name;
        public TextMeshProUGUI city_lv;
        public RectTransform location;
        public Image headIcon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(city_map_group), city_map_group);
            EnsureBound(nameof(city_img), city_img);
            EnsureBound(nameof(label_con), label_con);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(city_name), city_name);
            EnsureBound(nameof(city_lv), city_lv);
            EnsureBound(nameof(location), location);
            EnsureBound(nameof(headIcon), headIcon);
        }
    }
}
