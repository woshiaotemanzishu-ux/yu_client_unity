// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eyou/JumpToCommentView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eyou
{
    public partial class JumpToCommentViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_detail;
        public Image _img_close;
        public RectTransform _box_cancel;
        public TextMeshProUGUI _lb_cancel;
        public RectTransform _box_go;
        public TextMeshProUGUI _lb_go;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_detail), _img_detail);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_box_cancel), _box_cancel);
            EnsureBound(nameof(_lb_cancel), _lb_cancel);
            EnsureBound(nameof(_box_go), _box_go);
            EnsureBound(nameof(_lb_go), _lb_go);
        }
    }
}
