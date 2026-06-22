// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningBossDamageItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningBossDamageItemBind : BaseView
    {
        public Image img_rank;
        public TextMeshProUGUI _lb_rank;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_hurt;
        public Image img_line;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_rank), img_rank);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_hurt), _lb_hurt);
            EnsureBound(nameof(img_line), img_line);
        }
    }
}
