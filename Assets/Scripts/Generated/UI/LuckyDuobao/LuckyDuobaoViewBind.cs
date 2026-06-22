// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/luckyDuobao/LuckyDuobaoView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LuckyDuobao
{
    public partial class LuckyDuobaoViewBind : BaseView
    {
        public Image title_bg;
        public RectTransform _gp_time;
        public Image rewardBg;
        public ScrollRect gradeScroll;
        public RectTransform Content;
        public GameObject _tpl_LuckyDuobaoItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(title_bg), title_bg);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(rewardBg), rewardBg);
            EnsureBound(nameof(gradeScroll), gradeScroll);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_LuckyDuobaoItem), _tpl_LuckyDuobaoItem);
        }
    }
}
