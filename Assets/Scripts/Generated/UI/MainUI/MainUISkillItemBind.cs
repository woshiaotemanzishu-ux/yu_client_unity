// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUISkillItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUISkillItemBind : BaseView
    {
        public RectTransform con;
        public Image bg;
        public Image icon;
        public Image @lock;

        protected override void BindNodes()
        {
            EnsureBound(nameof(con), con);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(@lock), @lock);
        }
    }
}
