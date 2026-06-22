// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/functionOpen/FunctionOpenAutoView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FunctionOpen
{
    public partial class FunctionOpenAutoViewBind : BaseView
    {
        public RectTransform click_bg;
        public Image star_bg;
        public Image title;
        public Image icon_bg;
        public RectTransform effect;
        public RectTransform con;
        public Image content_bg;
        public TextMeshProUGUI tips;
        public Image skill_icon_bg;
        public Image icon;
        public RectTransform skill_icon;
        public ScrollRect _scroll;
        public RectTransform Content;
        public RectTransform okBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public Image iconDisplay;
        public TextMeshProUGUI close_tip;
        public TextMeshProUGUI skillLab;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click_bg), click_bg);
            EnsureBound(nameof(star_bg), star_bg);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(effect), effect);
            EnsureBound(nameof(con), con);
            EnsureBound(nameof(content_bg), content_bg);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(skill_icon_bg), skill_icon_bg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(skill_icon), skill_icon);
            EnsureBound(nameof(_scroll), _scroll);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(okBtn), okBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(iconDisplay), iconDisplay);
            EnsureBound(nameof(close_tip), close_tip);
            EnsureBound(nameof(skillLab), skillLab);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
