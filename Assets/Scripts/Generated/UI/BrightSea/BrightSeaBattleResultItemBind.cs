// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/brightSea/BrightSeaBattleResultItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BrightSea
{
    public partial class BrightSeaBattleResultItemBind : BaseView
    {
        public RectTransform _box_head;
        public Image _img_result;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_fight;
        public TextMeshProUGUI label;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_head), _box_head);
            EnsureBound(nameof(_img_result), _img_result);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_fight), _lb_fight);
            EnsureBound(nameof(label), label);
        }
    }
}
