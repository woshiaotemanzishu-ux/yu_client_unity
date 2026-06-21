// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonTalentItem2.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonTalentItem2Bind : BaseView
    {
        public Image item_bg;
        public Image _icon;
        public Image _icon_mask;
        public Image _add;
        public RectTransform _lock;
        public Image Image;
        public Image Image1;
        public Image _red;
        public Image _tips;
        public TextMeshProUGUI _lv;
        public GameObject _tpl_DemonTalentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(item_bg), item_bg);
            EnsureBound(nameof(_icon), _icon);
            EnsureBound(nameof(_icon_mask), _icon_mask);
            EnsureBound(nameof(_add), _add);
            EnsureBound(nameof(_lock), _lock);
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(Image1), Image1);
            EnsureBound(nameof(_red), _red);
            EnsureBound(nameof(_tips), _tips);
            EnsureBound(nameof(_lv), _lv);
            EnsureBound(nameof(_tpl_DemonTalentItem), _tpl_DemonTalentItem);
        }
    }
}
