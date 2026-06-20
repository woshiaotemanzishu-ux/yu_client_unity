// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/EmailItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class EmailItemBind : BaseView
    {
        public Image bg;
        public TextMeshProUGUI title;
        public TextMeshProUGUI time;
        public TextMeshProUGUI time2;
        public Image read;
        public Image unread;
        public Image rewardImg;
        public Image getImg;
        public RectTransform click;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(time), time);
            EnsureBound(nameof(time2), time2);
            EnsureBound(nameof(read), read);
            EnsureBound(nameof(unread), unread);
            EnsureBound(nameof(rewardImg), rewardImg);
            EnsureBound(nameof(getImg), getImg);
            EnsureBound(nameof(click), click);
        }
    }
}
