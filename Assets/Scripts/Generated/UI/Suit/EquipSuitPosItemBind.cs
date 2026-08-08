// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/suit/EquipSuitPosItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Suit
{
    public partial class EquipSuitPosItemBind : BaseView
    {
        public RectTransform itemBox;
        public Image defImg;
        public Image selectImg;
        public Image bgImg;
        public Image iconImg;
        public RectTransform iconBox;
        public TextMeshProUGUI nameLab;
        public TextMeshProUGUI descLab;
        public TextMeshProUGUI numLab;
        public Image redImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(itemBox), itemBox);
            EnsureBound(nameof(defImg), defImg);
            EnsureBound(nameof(selectImg), selectImg);
            EnsureBound(nameof(bgImg), bgImg);
            EnsureBound(nameof(iconImg), iconImg);
            EnsureBound(nameof(iconBox), iconBox);
            EnsureBound(nameof(nameLab), nameLab);
            EnsureBound(nameof(descLab), descLab);
            EnsureBound(nameof(numLab), numLab);
            EnsureBound(nameof(redImg), redImg);
        }
    }
}
