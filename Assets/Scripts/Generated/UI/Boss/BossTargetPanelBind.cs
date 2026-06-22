// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/boss/BossTargetPanel.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Boss
{
    public partial class BossTargetPanelBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _num_lb;
        public ScrollRect _scroller;
        public RectTransform Content;
        public Image _img;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_num_lb), _num_lb);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_img), _img);
        }
    }
}
