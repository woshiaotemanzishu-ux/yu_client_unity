// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/daily/DailyLivenessRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Daily
{
    public partial class DailyLivenessRewardItemBind : BaseView
    {
        public Image bg;
        public RectTransform conta;
        public Image can_got;
        public RectTransform effect_conta;
        public Image got_tag;
        public RectTransform click_bg;
        public Image week_img;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(conta), conta);
            EnsureBound(nameof(can_got), can_got);
            EnsureBound(nameof(effect_conta), effect_conta);
            EnsureBound(nameof(got_tag), got_tag);
            EnsureBound(nameof(click_bg), click_bg);
            EnsureBound(nameof(week_img), week_img);
        }
    }
}
