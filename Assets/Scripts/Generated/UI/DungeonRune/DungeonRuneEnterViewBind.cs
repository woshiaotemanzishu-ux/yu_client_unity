// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonRune/DungeonRuneEnterView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonRune
{
    public partial class DungeonRuneEnterViewBind : BaseView
    {
        public ScrollRect _list_bg;
        public ScrollRect _list_line;
        public ScrollRect _list_item;
        public RectTransform _gp_unlock;
        public Image _img_unlock_bg;
        public RectTransform _box_unlock_effect;
        public Image _img_icon;
        public TextMeshProUGUI _lb_icon_name;
        public Image _img_unlock_tips;
        public Image _img_unlock_bg2;
        public TextMeshProUGUI _lb_unlock_msg;
        public RectTransform _box_unlock_click;
        public Image _img_unlock_red;
        public RectTransform _gp_rec_fight;
        public Image _img_rec_fight_bg;
        public TextMeshProUGUI _lb_rec_desc;
        public TextMeshProUGUI _lb_rec_fight;
        public TextMeshProUGUI _lb_done;
        public RectTransform _gp_challenge;
        public Image _img_challenge;
        public TextMeshProUGUI _lb_challenge;
        public Image _img_challenge_red;
        public RectTransform _gp_daily_reward;
        public Image _img_daily_reward;
        public TextMeshProUGUI _lb_daily_reward;
        public Image _img_daily_reward_red;
        public RectTransform _gp_first;
        public Image _img_first;
        public TextMeshProUGUI _lb_first;
        public Image _img_first_red;
        public RectTransform _gp_target;
        public Image _img_target;
        public TextMeshProUGUI _lb_target;
        public Image _img_target_red;
        public RectTransform _gp_reward;
        public Image _img_reward_bg;
        public TextMeshProUGUI _lb_reward_floor;
        public ScrollRect _list_reward;
        public RectTransform giftIcon;
        public GameObject _tpl_DungeonRuneEnterBgItem;
        public GameObject _tpl_DungeonRuneEnterItem;
        public GameObject _tpl_DungeonRuneEnterLineItem;
        public GameObject _tpl_CommonRewardItem;
        public GameObject _tpl_GiftPushIcon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_list_bg), _list_bg);
            EnsureBound(nameof(_list_line), _list_line);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_gp_unlock), _gp_unlock);
            EnsureBound(nameof(_img_unlock_bg), _img_unlock_bg);
            EnsureBound(nameof(_box_unlock_effect), _box_unlock_effect);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_lb_icon_name), _lb_icon_name);
            EnsureBound(nameof(_img_unlock_tips), _img_unlock_tips);
            EnsureBound(nameof(_img_unlock_bg2), _img_unlock_bg2);
            EnsureBound(nameof(_lb_unlock_msg), _lb_unlock_msg);
            EnsureBound(nameof(_box_unlock_click), _box_unlock_click);
            EnsureBound(nameof(_img_unlock_red), _img_unlock_red);
            EnsureBound(nameof(_gp_rec_fight), _gp_rec_fight);
            EnsureBound(nameof(_img_rec_fight_bg), _img_rec_fight_bg);
            EnsureBound(nameof(_lb_rec_desc), _lb_rec_desc);
            EnsureBound(nameof(_lb_rec_fight), _lb_rec_fight);
            EnsureBound(nameof(_lb_done), _lb_done);
            EnsureBound(nameof(_gp_challenge), _gp_challenge);
            EnsureBound(nameof(_img_challenge), _img_challenge);
            EnsureBound(nameof(_lb_challenge), _lb_challenge);
            EnsureBound(nameof(_img_challenge_red), _img_challenge_red);
            EnsureBound(nameof(_gp_daily_reward), _gp_daily_reward);
            EnsureBound(nameof(_img_daily_reward), _img_daily_reward);
            EnsureBound(nameof(_lb_daily_reward), _lb_daily_reward);
            EnsureBound(nameof(_img_daily_reward_red), _img_daily_reward_red);
            EnsureBound(nameof(_gp_first), _gp_first);
            EnsureBound(nameof(_img_first), _img_first);
            EnsureBound(nameof(_lb_first), _lb_first);
            EnsureBound(nameof(_img_first_red), _img_first_red);
            EnsureBound(nameof(_gp_target), _gp_target);
            EnsureBound(nameof(_img_target), _img_target);
            EnsureBound(nameof(_lb_target), _lb_target);
            EnsureBound(nameof(_img_target_red), _img_target_red);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_img_reward_bg), _img_reward_bg);
            EnsureBound(nameof(_lb_reward_floor), _lb_reward_floor);
            EnsureBound(nameof(_list_reward), _list_reward);
            EnsureBound(nameof(giftIcon), giftIcon);
            EnsureBound(nameof(_tpl_DungeonRuneEnterBgItem), _tpl_DungeonRuneEnterBgItem);
            EnsureBound(nameof(_tpl_DungeonRuneEnterItem), _tpl_DungeonRuneEnterItem);
            EnsureBound(nameof(_tpl_DungeonRuneEnterLineItem), _tpl_DungeonRuneEnterLineItem);
            EnsureBound(nameof(_tpl_CommonRewardItem), _tpl_CommonRewardItem);
            EnsureBound(nameof(_tpl_GiftPushIcon), _tpl_GiftPushIcon);
        }
    }
}
