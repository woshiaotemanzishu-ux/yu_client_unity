// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/levelReward/LevelReward.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LevelReward
{
    public partial class LevelRewardBind : BaseView
    {
        public RectTransform _gp_reward;
        public Image _img_mask;
        public Image _img_draw;
        public Image _img_limit;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_img_mask), _img_mask);
            EnsureBound(nameof(_img_draw), _img_draw);
            EnsureBound(nameof(_img_limit), _img_limit);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
