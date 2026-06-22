// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnChallengerMsgItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnChallengerMsgItemBind : BaseView
    {
        public Image _bg;
        public RectTransform role_head;
        public TextMeshProUGUI role_name;
        public TextMeshProUGUI fight;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(role_head), role_head);
            EnsureBound(nameof(role_name), role_name);
            EnsureBound(nameof(fight), fight);
        }
    }
}
