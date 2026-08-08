// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/suit/EquipSuitPreviewTips.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Suit
{
    public partial class EquipSuitPreviewTipsBind : BaseView
    {
        public Image bgImg;
        public RectTransform closeBtn;
        public RectTransform iconBox;
        public Image bgImg1;
        public TextMeshProUGUI descLab;
        public RectTransform effBox;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bgImg), bgImg);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(iconBox), iconBox);
            EnsureBound(nameof(bgImg1), bgImg1);
            EnsureBound(nameof(descLab), descLab);
            EnsureBound(nameof(effBox), effBox);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
