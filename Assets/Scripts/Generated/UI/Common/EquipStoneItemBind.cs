// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/EquipStoneItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class EquipStoneItemBind : BaseView
    {
        public RectTransform conta;
        public Image stone_bg;
        public Image stone_icon;
        public Image stone_lock;
        public TextMeshProUGUI stone_name;
        public TextMeshProUGUI stone_label;

        protected override void BindNodes()
        {
            EnsureBound(nameof(conta), conta);
            EnsureBound(nameof(stone_bg), stone_bg);
            EnsureBound(nameof(stone_icon), stone_icon);
            EnsureBound(nameof(stone_lock), stone_lock);
            EnsureBound(nameof(stone_name), stone_name);
            EnsureBound(nameof(stone_label), stone_label);
        }
    }
}
