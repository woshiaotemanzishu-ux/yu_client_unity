// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/CompositeGodBefallItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class CompositeGodBefallItemBind : BaseView
    {
        public RectTransform gp_con;
        public Image bg;
        public RectTransform item_group;
        public Image lock_img;
        public Image add_img;
        public RectTransform gp_num;
        public TextMeshProUGUI num_label;
        public Image _img;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_con), gp_con);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(item_group), item_group);
            EnsureBound(nameof(lock_img), lock_img);
            EnsureBound(nameof(add_img), add_img);
            EnsureBound(nameof(gp_num), gp_num);
            EnsureBound(nameof(num_label), num_label);
            EnsureBound(nameof(_img), _img);
        }
    }
}
