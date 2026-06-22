// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/luckyDuobao/LuckyDuobaoItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LuckyDuobao
{
    public partial class LuckyDuobaoItemBind : BaseView
    {
        public Image bg;
        public Image desc_bg;
        public TextMeshProUGUI desc;
        public ScrollRect rewardScroll;
        public RectTransform Content;
        public Image btn;
        public TextMeshProUGUI btn_name;
        public Image red;
        public Image draw;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(desc_bg), desc_bg);
            EnsureBound(nameof(desc), desc);
            EnsureBound(nameof(rewardScroll), rewardScroll);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(btn), btn);
            EnsureBound(nameof(btn_name), btn_name);
            EnsureBound(nameof(red), red);
            EnsureBound(nameof(draw), draw);
        }
    }
}
