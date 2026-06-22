// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonCommon/DungeonFightSceneMaskView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonCommon
{
    public partial class DungeonFightSceneMaskViewBind : BaseView
    {
        public Image _img_mask_top;
        public Image _img_mask_down;
        public RectTransform _gp_born_effect;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_mask_top), _img_mask_top);
            EnsureBound(nameof(_img_mask_down), _img_mask_down);
            EnsureBound(nameof(_gp_born_effect), _gp_born_effect);
        }
    }
}
