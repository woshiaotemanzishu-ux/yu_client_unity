// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/luckyTreasure/LTItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LuckyTreasure
{
    public partial class LTItemBind : BaseView
    {
        public Image _bg;
        public RectTransform _item;
        public Image _rare;
        public Image _diam;
        public Image _desc;
        public TextMeshProUGUI _num;
        public RectTransform _rare_eff;
        public RectTransform bg_effect;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_item), _item);
            EnsureBound(nameof(_rare), _rare);
            EnsureBound(nameof(_diam), _diam);
            EnsureBound(nameof(_desc), _desc);
            EnsureBound(nameof(_num), _num);
            EnsureBound(nameof(_rare_eff), _rare_eff);
            EnsureBound(nameof(bg_effect), bg_effect);
        }
    }
}
