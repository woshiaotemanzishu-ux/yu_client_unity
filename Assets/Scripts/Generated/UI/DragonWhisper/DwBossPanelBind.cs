// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonWhisper/dwBossPanel.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonWhisper
{
    public partial class DwBossPanelBind : BaseView
    {
        public Image _panel_btn;
        public RectTransform _Group4;
        public Image _img_bg;
        public Image _img_line;
        public Image _panel_img;
        public TextMeshProUGUI _lb_title;
        public ScrollRect _scroll_boss;
        public TextMeshProUGUI _lb_tips;
        public TextMeshProUGUI _lb_time;
        public Image _btn_add;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_panel_btn), _panel_btn);
            EnsureBound(nameof(_Group4), _Group4);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_line), _img_line);
            EnsureBound(nameof(_panel_img), _panel_img);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_scroll_boss), _scroll_boss);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_btn_add), _btn_add);
        }
    }
}
