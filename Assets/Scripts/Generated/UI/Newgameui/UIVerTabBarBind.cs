// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/newgameui/UIVerTabBar.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Newgameui
{
    public partial class UIVerTabBarBind : BaseView
    {
        public ScrollRect scroll;
        public RectTransform scroll_group;
        public Image _img_arrow;
        public Image img_red;
        public GameObject _tpl_UIVerTabBtn;

        protected override void BindNodes()
        {
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(scroll_group), scroll_group);
            EnsureBound(nameof(_img_arrow), _img_arrow);
            EnsureBound(nameof(img_red), img_red);
            EnsureBound(nameof(_tpl_UIVerTabBtn), _tpl_UIVerTabBtn);
        }
    }
}
