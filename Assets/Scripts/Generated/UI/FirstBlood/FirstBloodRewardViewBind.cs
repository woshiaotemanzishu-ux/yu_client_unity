// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/firstBlood/FirstBloodRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FirstBlood
{
    public partial class FirstBloodRewardViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image5;
        public Image image;
        public Image close_img;
        public TextMeshProUGUI title_text;
        public ScrollRect ScrollView;
        public RectTransform Content;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(image), image);
            EnsureBound(nameof(close_img), close_img);
            EnsureBound(nameof(title_text), title_text);
            EnsureBound(nameof(ScrollView), ScrollView);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
