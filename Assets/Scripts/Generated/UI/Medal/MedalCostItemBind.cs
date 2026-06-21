// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/medal/MedalCostItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Medal
{
    public partial class MedalCostItemBind : BaseView
    {
        public RectTransform _box;
        public Image lImg;
        public Image hImg;
        public TextMeshProUGUI descLab;
        public RectTransform iconBox;
        public TextMeshProUGUI curNum;
        public TextMeshProUGUI nextLab;
        public Image gouImg;
        public Image chaImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box), _box);
            EnsureBound(nameof(lImg), lImg);
            EnsureBound(nameof(hImg), hImg);
            EnsureBound(nameof(descLab), descLab);
            EnsureBound(nameof(iconBox), iconBox);
            EnsureBound(nameof(curNum), curNum);
            EnsureBound(nameof(nextLab), nextLab);
            EnsureBound(nameof(gouImg), gouImg);
            EnsureBound(nameof(chaImg), chaImg);
        }
    }
}
