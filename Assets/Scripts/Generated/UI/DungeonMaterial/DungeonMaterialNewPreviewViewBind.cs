// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonMaterial/DungeonMaterialNewPreviewView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonMaterial
{
    public partial class DungeonMaterialNewPreviewViewBind : BaseView
    {
        public Image _bg;
        public Image _img_h_bg;
        public Image _img_close;
        public TextMeshProUGUI _text_1;
        public TextMeshProUGUI _text_2;
        public TextMeshProUGUI _text_3;
        public TextMeshProUGUI _text_fight;
        public TextMeshProUGUI _text_4;
        public ScrollRect _scroll_reward;
        public RectTransform Content;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_img_h_bg), _img_h_bg);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_text_1), _text_1);
            EnsureBound(nameof(_text_2), _text_2);
            EnsureBound(nameof(_text_3), _text_3);
            EnsureBound(nameof(_text_fight), _text_fight);
            EnsureBound(nameof(_text_4), _text_4);
            EnsureBound(nameof(_scroll_reward), _scroll_reward);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
