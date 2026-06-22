// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dailyRecharge/DailyFirstReward.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DailyRecharge
{
    public partial class DailyFirstRewardBind : BaseView
    {
        public TextMeshProUGUI _lb_dec;
        public Image money;
        public Image _Image1;
        public Image _img_progress;
        public RectTransform _gp_reward;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_lb_dec), _lb_dec);
            EnsureBound(nameof(money), money);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_progress), _img_progress);
            EnsureBound(nameof(_gp_reward), _gp_reward);
        }
    }
}
