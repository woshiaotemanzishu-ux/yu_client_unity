// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/functionOpen/FunctionOpenListItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FunctionOpen
{
    public partial class FunctionOpenListItemBind : BaseView
    {
        public Image bg;
        public Image img2;
        public TextMeshProUGUI label1;
        public TextMeshProUGUI label2;
        public RectTransform effect;
        public Image icon_bg;
        public Image icon;
        public RectTransform reward_con;
        public RectTransform con;
        public RectTransform click_bg;
        public Image red_dot;
        public RectTransform goBtn;
        public Image _Image1;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI name_txt;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(img2), img2);
            EnsureBound(nameof(label1), label1);
            EnsureBound(nameof(label2), label2);
            EnsureBound(nameof(effect), effect);
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(reward_con), reward_con);
            EnsureBound(nameof(con), con);
            EnsureBound(nameof(click_bg), click_bg);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(goBtn), goBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(name_txt), name_txt);
        }
    }
}
