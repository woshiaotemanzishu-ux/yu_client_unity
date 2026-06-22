// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfSingleRank/kfSRSceneTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfSingleRank
{
    public partial class KfSRSceneTipsViewBind : BaseView
    {
        public RectTransform _gp_tips;
        public Image _img_black;
        public Image _img_title;
        public Image _img_start;
        public RectTransform _gp_title;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_tips), _gp_tips);
            EnsureBound(nameof(_img_black), _img_black);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_img_start), _img_start);
            EnsureBound(nameof(_gp_title), _gp_title);
        }
    }
}
