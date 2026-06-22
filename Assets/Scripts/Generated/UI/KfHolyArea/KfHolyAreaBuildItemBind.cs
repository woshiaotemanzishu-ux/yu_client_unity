// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaBuildItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaBuildItemBind : BaseView
    {
        public Image _img_scene;
        public Image _server_bg;
        public TextMeshProUGUI _lb_server;
        public RectTransform desc_gp;
        public TextMeshProUGUI _lb_desc;
        public Image _img_progress;
        public TextMeshProUGUI _lb_score;
        public TextMeshProUGUI _lb_desc1;
        public RectTransform _gp_box;
        public RectTransform _gp_behind;
        public Image _img_box;
        public RectTransform _gp_front;
        public RectTransform _gp_effect;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_scene), _img_scene);
            EnsureBound(nameof(_server_bg), _server_bg);
            EnsureBound(nameof(_lb_server), _lb_server);
            EnsureBound(nameof(desc_gp), desc_gp);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_img_progress), _img_progress);
            EnsureBound(nameof(_lb_score), _lb_score);
            EnsureBound(nameof(_lb_desc1), _lb_desc1);
            EnsureBound(nameof(_gp_box), _gp_box);
            EnsureBound(nameof(_gp_behind), _gp_behind);
            EnsureBound(nameof(_img_box), _img_box);
            EnsureBound(nameof(_gp_front), _gp_front);
            EnsureBound(nameof(_gp_effect), _gp_effect);
        }
    }
}
