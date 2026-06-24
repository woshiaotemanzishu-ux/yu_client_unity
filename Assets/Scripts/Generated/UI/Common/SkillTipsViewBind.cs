// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/SkillTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class SkillTipsViewBind : BaseView
    {
        public Image skill_bg;
        public Image icon_bg;
        public Image icon;
        public TextMeshProUGUI name_text;
        public TextMeshProUGUI lv_text;
        public RectTransform enter_btn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public ScrollRect des_scroller;
        public RectTransform Content;
        public TextMeshProUGUI des_text;
        public Image img_title;
        public TextMeshProUGUI _Label1;

        protected override void BindNodes()
        {
            EnsureBound(nameof(skill_bg), skill_bg);
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(name_text), name_text);
            EnsureBound(nameof(lv_text), lv_text);
            EnsureBound(nameof(enter_btn), enter_btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(des_scroller), des_scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(des_text), des_text);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(_Label1), _Label1);
        }
    }
}
