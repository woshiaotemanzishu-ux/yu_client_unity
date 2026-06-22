// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/boss/BossDropRecordItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Boss
{
    public partial class BossDropRecordItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_top;
        public TextMeshProUGUI _lb_time;
        public TextMeshProUGUI _lb_time2;
        public RectTransform _box_reward;
        public TextMeshProUGUI _lb_dunName;
        public TextMeshProUGUI _lb_bossName;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp_point;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_top), _img_top);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_lb_time2), _lb_time2);
            EnsureBound(nameof(_box_reward), _box_reward);
            EnsureBound(nameof(_lb_dunName), _lb_dunName);
            EnsureBound(nameof(_lb_bossName), _lb_bossName);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp_point), _gp_point);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
