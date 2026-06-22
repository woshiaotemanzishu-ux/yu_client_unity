// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/attention/AttentionViewLaya.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Attention
{
    public partial class AttentionViewLayaBind : BaseView
    {
        public Image bg;
        public Image title;
        public Image bg1;
        public Image link;
        public Image bg3;
        public RectTransform Content;
        public Image bg2;
        public TextMeshProUGUI des;
        public RectTransform copy;
        public Image close;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(link), link);
            EnsureBound(nameof(bg3), bg3);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(bg2), bg2);
            EnsureBound(nameof(des), des);
            EnsureBound(nameof(copy), copy);
            EnsureBound(nameof(close), close);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
