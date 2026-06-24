// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/InstructionView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class InstructionViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_close;
        public ScrollRect _panel_item;
        public RectTransform _vbox_con;
        public TextMeshProUGUI _lb_ins;
        public TextMeshProUGUI _lb_title;
        public GameObject _tpl_InstructionItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_panel_item), _panel_item);
            EnsureBound(nameof(_vbox_con), _vbox_con);
            EnsureBound(nameof(_lb_ins), _lb_ins);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_tpl_InstructionItem), _tpl_InstructionItem);
        }
    }
}
