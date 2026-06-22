// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningBossPanel.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningBossPanelBind : BaseView
    {
        public RectTransform _Group3;
        public RectTransform _panel_gp;
        public RectTransform _Group2;
        public Image Image;
        public ScrollRect _scroller;
        public RectTransform Content;
        public Image _panel_img;
        public Image _panel_btn;
        public TextMeshProUGUI Text;
        public RectTransform _box_rank;
        public Image img_rank;
        public GameObject _tpl_EveningBossDamageItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group3), _Group3);
            EnsureBound(nameof(_panel_gp), _panel_gp);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_panel_img), _panel_img);
            EnsureBound(nameof(_panel_btn), _panel_btn);
            EnsureBound(nameof(Text), Text);
            EnsureBound(nameof(_box_rank), _box_rank);
            EnsureBound(nameof(img_rank), img_rank);
            EnsureBound(nameof(_tpl_EveningBossDamageItem), _tpl_EveningBossDamageItem);
        }
    }
}
