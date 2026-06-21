// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/InstructionItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class InstructionItemBind : BaseView
    {
        public RectTransform _box_title;
        public Image _img_bg;
        public TextMeshProUGUI _html_title;
        public RectTransform _vbox_con;
        public GameObject _tpl_InstructionSmallItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_title), _box_title);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_html_title), _html_title);
            EnsureBound(nameof(_vbox_con), _vbox_con);
            EnsureBound(nameof(_tpl_InstructionSmallItem), _tpl_InstructionSmallItem);
        }
    }
}
