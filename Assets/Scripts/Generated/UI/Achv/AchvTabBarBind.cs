// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/achv/AchvTabBar.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Achv
{
    public partial class AchvTabBarBind : BaseView
    {
        public ScrollRect scroll;
        public RectTransform Content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(Content), Content);
        }
    }
}
