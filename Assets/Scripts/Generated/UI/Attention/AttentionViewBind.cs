// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/attention/AttentionView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Attention
{
    public partial class AttentionViewBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _panel_reward;
        public RectTransform _hbox_reward;
        public RectTransform _box_attention;
        public Image _img_attention_bg;
        public TextMeshProUGUI _lb_attention;
        public Image _img_attention_red;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_panel_reward), _panel_reward);
            EnsureBound(nameof(_hbox_reward), _hbox_reward);
            EnsureBound(nameof(_box_attention), _box_attention);
            EnsureBound(nameof(_img_attention_bg), _img_attention_bg);
            EnsureBound(nameof(_lb_attention), _lb_attention);
            EnsureBound(nameof(_img_attention_red), _img_attention_red);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
