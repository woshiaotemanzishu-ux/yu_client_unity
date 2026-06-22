// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dress/DressItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Dress
{
    public partial class DressItemBind : BaseView
    {
        public Image bg;
        public Image gray_bg;
        public Image select_bg;
        public Image red_dot;
        public Image head;
        public RectTransform con;
        public Image use_tag;
        public TextMeshProUGUI dress_name;
        public RectTransform click_bg;
        public TextMeshProUGUI fight;
        public TextMeshProUGUI _lb_fight;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(gray_bg), gray_bg);
            EnsureBound(nameof(select_bg), select_bg);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(head), head);
            EnsureBound(nameof(con), con);
            EnsureBound(nameof(use_tag), use_tag);
            EnsureBound(nameof(dress_name), dress_name);
            EnsureBound(nameof(click_bg), click_bg);
            EnsureBound(nameof(fight), fight);
            EnsureBound(nameof(_lb_fight), _lb_fight);
        }
    }
}
