// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guard/GuardActivateItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guard
{
    public partial class GuardActivateItemBind : BaseView
    {
        public TextMeshProUGUI lb_desc;
        public Image img_attrName;
        public Image img_num;
        public Image img_arr;
        public RectTransform gp_model;

        protected override void BindNodes()
        {
            EnsureBound(nameof(lb_desc), lb_desc);
            EnsureBound(nameof(img_attrName), img_attrName);
            EnsureBound(nameof(img_num), img_num);
            EnsureBound(nameof(img_arr), img_arr);
            EnsureBound(nameof(gp_model), gp_model);
        }
    }
}
