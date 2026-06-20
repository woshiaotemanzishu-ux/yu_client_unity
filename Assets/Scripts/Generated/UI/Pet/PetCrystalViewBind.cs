// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/pet/PetCrystalView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Pet
{
    public partial class PetCrystalViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI quantity_text;
        public TextMeshProUGUI _Label1;
        public Image _Image11;
        public RectTransform _Group1;
        public Image _Image3;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(quantity_text), quantity_text);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image3), _Image3);
        }
    }
}
