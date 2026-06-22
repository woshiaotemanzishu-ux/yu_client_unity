// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonBallGift/DragonBallGiftView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonBallGift
{
    public partial class DragonBallGiftViewBind : BaseView
    {
        public Image img_title_bg;
        public Image img_title;
        public RectTransform box_reward;
        public Image img_reward_bg;
        public Image img_reward;
        public Image img_price;
        public Image img_desc;
        public RectTransform box_fight;
        public Image img_close;
        public Image _tip_accu;
        public Image img_buy;
        public TextMeshProUGUI lable_price;
        public TextMeshProUGUI html_buy_times;
        public RectTransform box_middle_reward;
        public GameObject _tpl_EquipmentItem;
        public GameObject _tpl_FightingShowSmallItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_title_bg), img_title_bg);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(box_reward), box_reward);
            EnsureBound(nameof(img_reward_bg), img_reward_bg);
            EnsureBound(nameof(img_reward), img_reward);
            EnsureBound(nameof(img_price), img_price);
            EnsureBound(nameof(img_desc), img_desc);
            EnsureBound(nameof(box_fight), box_fight);
            EnsureBound(nameof(img_close), img_close);
            EnsureBound(nameof(_tip_accu), _tip_accu);
            EnsureBound(nameof(img_buy), img_buy);
            EnsureBound(nameof(lable_price), lable_price);
            EnsureBound(nameof(html_buy_times), html_buy_times);
            EnsureBound(nameof(box_middle_reward), box_middle_reward);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
            EnsureBound(nameof(_tpl_FightingShowSmallItem), _tpl_FightingShowSmallItem);
        }
    }
}
