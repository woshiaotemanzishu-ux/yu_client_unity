// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarScenePanel.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarScenePanelBind : BaseView
    {
        public RectTransform _panel_gp;
        public Image _img_bg;
        public Image _img_jiejie_b;
        public TextMeshProUGUI _txt_title1;
        public RectTransform _Group1;
        public RectTransform _rbtn_boss;
        public Image img_rbtn_boss;
        public TextMeshProUGUI lb_rbtn_boss;
        public RectTransform _rbtn_damage;
        public Image img_rbtn_damage;
        public TextMeshProUGUI lb_rbtn_damage;
        public TextMeshProUGUI _lb_relive;
        public ScrollRect Content;
        public ScrollRect _scroller;
        public RectTransform dContent;
        public Image _panel_btn;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_panel_gp), _panel_gp);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_jiejie_b), _img_jiejie_b);
            EnsureBound(nameof(_txt_title1), _txt_title1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_rbtn_boss), _rbtn_boss);
            EnsureBound(nameof(img_rbtn_boss), img_rbtn_boss);
            EnsureBound(nameof(lb_rbtn_boss), lb_rbtn_boss);
            EnsureBound(nameof(_rbtn_damage), _rbtn_damage);
            EnsureBound(nameof(img_rbtn_damage), img_rbtn_damage);
            EnsureBound(nameof(lb_rbtn_damage), lb_rbtn_damage);
            EnsureBound(nameof(_lb_relive), _lb_relive);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(dContent), dContent);
            EnsureBound(nameof(_panel_btn), _panel_btn);
        }
    }
}
