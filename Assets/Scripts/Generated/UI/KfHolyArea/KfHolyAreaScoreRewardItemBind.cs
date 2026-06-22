// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaScoreRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaScoreRewardItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform _gp_behind;
        public Image _img_box;
        public RectTransform _gp_front;
        public TextMeshProUGUI _lb_score;
        public Image _img_get;
        public Image red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_gp_behind), _gp_behind);
            EnsureBound(nameof(_img_box), _img_box);
            EnsureBound(nameof(_gp_front), _gp_front);
            EnsureBound(nameof(_lb_score), _lb_score);
            EnsureBound(nameof(_img_get), _img_get);
            EnsureBound(nameof(red), red);
        }
    }
}
