// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossField/BossFieldEnterView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossField
{
    public partial class BossFieldEnterViewBind : BaseView
    {
        public RectTransform _box_model;
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_bg3;
        public Image _img_bg4;
        public Image _img_drop;
        public Image _img_shop;
        public Image _img_shop_red;
        public Image _img_attention;
        public TextMeshProUGUI _lb_boss_level;
        public TextMeshProUGUI _lb_boss_name;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _lb_attention;
        public TextMeshProUGUI _lb_first_blood;
        public TextMeshProUGUI _lb_drop_tips;
        public TextMeshProUGUI _lb_vit;
        public RectTransform _box_room;
        public ScrollRect _panel_reward;
        public GameObject _tpl_BossFieldRoomItem;
        public GameObject _tpl_BossFieldRewardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_model), _box_model);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_img_bg4), _img_bg4);
            EnsureBound(nameof(_img_drop), _img_drop);
            EnsureBound(nameof(_img_shop), _img_shop);
            EnsureBound(nameof(_img_shop_red), _img_shop_red);
            EnsureBound(nameof(_img_attention), _img_attention);
            EnsureBound(nameof(_lb_boss_level), _lb_boss_level);
            EnsureBound(nameof(_lb_boss_name), _lb_boss_name);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_lb_attention), _lb_attention);
            EnsureBound(nameof(_lb_first_blood), _lb_first_blood);
            EnsureBound(nameof(_lb_drop_tips), _lb_drop_tips);
            EnsureBound(nameof(_lb_vit), _lb_vit);
            EnsureBound(nameof(_box_room), _box_room);
            EnsureBound(nameof(_panel_reward), _panel_reward);
            EnsureBound(nameof(_tpl_BossFieldRoomItem), _tpl_BossFieldRoomItem);
            EnsureBound(nameof(_tpl_BossFieldRewardItem), _tpl_BossFieldRewardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
