// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossdomain/BossDomainScenePanel.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bossdomain
{
    public partial class BossDomainScenePanelBind : BaseView
    {
        public Image _img_bg;
        public Image _Image2_title2;
        public TextMeshProUGUI _Label4;
        public ScrollRect _sc_panel;
        public TextMeshProUGUI _lb_desc;
        public RectTransform _box_intro;
        public Image _img_intro;
        public TextMeshProUGUI _lb_intro;
        public RectTransform _box_help;
        public Image _img_help;
        public TextMeshProUGUI _lb_help;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Image2_title2), _Image2_title2);
            EnsureBound(nameof(_Label4), _Label4);
            EnsureBound(nameof(_sc_panel), _sc_panel);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_box_intro), _box_intro);
            EnsureBound(nameof(_img_intro), _img_intro);
            EnsureBound(nameof(_lb_intro), _lb_intro);
            EnsureBound(nameof(_box_help), _box_help);
            EnsureBound(nameof(_img_help), _img_help);
            EnsureBound(nameof(_lb_help), _lb_help);
        }
    }
}
