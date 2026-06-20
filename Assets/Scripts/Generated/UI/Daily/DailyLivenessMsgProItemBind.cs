// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/daily/DailyLivenessMsgProItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Daily
{
    public partial class DailyLivenessMsgProItemBind : BaseView
    {
        public TextMeshProUGUI pro;
        public TextMeshProUGUI next_pro;
        public Image _Image1;

        protected override void BindNodes()
        {
            EnsureBound(nameof(pro), pro);
            EnsureBound(nameof(next_pro), next_pro);
            EnsureBound(nameof(_Image1), _Image1);
        }
    }
}
