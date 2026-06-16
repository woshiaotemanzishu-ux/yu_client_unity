// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIFightModeView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIFightModeViewBind : BaseView
    {
        public Image image1;
        public Image image2;
        public TextMeshProUGUI label1;
        public RectTransform _gp_item;
        public GameObject _tpl_MainUIFightModeItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(image1), image1);
            EnsureBound(nameof(image2), image2);
            EnsureBound(nameof(label1), label1);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_tpl_MainUIFightModeItem), _tpl_MainUIFightModeItem);
        }
    }
}
