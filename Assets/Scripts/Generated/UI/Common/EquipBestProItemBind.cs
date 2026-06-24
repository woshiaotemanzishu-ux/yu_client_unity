// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/EquipBestProItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class EquipBestProItemBind : BaseView
    {
        public Image star;
        public TextMeshProUGUI pro;

        protected override void BindNodes()
        {
            EnsureBound(nameof(star), star);
            EnsureBound(nameof(pro), pro);
        }
    }
}
