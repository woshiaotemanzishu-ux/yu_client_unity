// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonHeart/DungeonHeartFightSceneItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonHeart
{
    public partial class DungeonHeartFightSceneItemBind : BaseView
    {
        public RectTransform _box_ui;
        public Image _img_bg;
        public Image _Image2_title2;
        public TextMeshProUGUI _lb_title2;
        public TextMeshProUGUI _lb_name;
        public Image _Image2;
        public Image _Image3;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public RectTransform _box_icon;
        public Image skillBg;
        public Image _img_skill_icon;
        public TextMeshProUGUI _lb_skill_desc;
        public RectTransform _img_arrow;
        public Image _panel_btn_img;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_ui), _box_ui);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Image2_title2), _Image2_title2);
            EnsureBound(nameof(_lb_title2), _lb_title2);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_box_icon), _box_icon);
            EnsureBound(nameof(skillBg), skillBg);
            EnsureBound(nameof(_img_skill_icon), _img_skill_icon);
            EnsureBound(nameof(_lb_skill_desc), _lb_skill_desc);
            EnsureBound(nameof(_img_arrow), _img_arrow);
            EnsureBound(nameof(_panel_btn_img), _panel_btn_img);
        }
    }
}
