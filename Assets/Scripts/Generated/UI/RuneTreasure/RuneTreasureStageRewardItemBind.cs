// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/runeTreasure/RuneTreasureStageRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.RuneTreasure
{
    public partial class RuneTreasureStageRewardItemBind : BaseView
    {
        public RectTransform conta;
        public RectTransform effect_group;
        public TextMeshProUGUI need_times;
        public Image click_bg;
        public Image got_tag;

        protected override void BindNodes()
        {
            EnsureBound(nameof(conta), conta);
            EnsureBound(nameof(effect_group), effect_group);
            EnsureBound(nameof(need_times), need_times);
            EnsureBound(nameof(click_bg), click_bg);
            EnsureBound(nameof(got_tag), got_tag);
        }
    }
}
