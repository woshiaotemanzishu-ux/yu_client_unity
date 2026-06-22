// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildSkillView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildSkillViewBind : BaseView
    {
        public Image _img_bg;
        public Image _icon_bg;
        public Image _img_title_bg;
        public Image _content_bg;
        public Image _text_bg;
        public RectTransform _btn_up_box;
        public Image _btn_up;
        public RectTransform _btn_down_box;
        public Image _btn_down;
        public TextMeshProUGUI _lb_skill_name;
        public TextMeshProUGUI _lb_max_tip;
        public TextMeshProUGUI _lb_lock_tip;
        public RectTransform _btn_levelup;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public Image _Image1;
        public RectTransform _group_item;
        public RectTransform _group_attr_cur;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _lb_cur_attr;
        public RectTransform _group_attr_next;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _lb_next_attr;
        public RectTransform _Group1;
        public TextMeshProUGUI _tx_desc1;
        public Image _img_icon;
        public TextMeshProUGUI _tx_desc2;
        public TextMeshProUGUI _tx_desc3;
        public ScrollRect _scroller;
        public RectTransform Content;
        public RectTransform _fight_con;
        public GameObject _tpl_GuildSkillShowItem;
        public GameObject _tpl_FightingShowSmallItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_icon_bg), _icon_bg);
            EnsureBound(nameof(_img_title_bg), _img_title_bg);
            EnsureBound(nameof(_content_bg), _content_bg);
            EnsureBound(nameof(_text_bg), _text_bg);
            EnsureBound(nameof(_btn_up_box), _btn_up_box);
            EnsureBound(nameof(_btn_up), _btn_up);
            EnsureBound(nameof(_btn_down_box), _btn_down_box);
            EnsureBound(nameof(_btn_down), _btn_down);
            EnsureBound(nameof(_lb_skill_name), _lb_skill_name);
            EnsureBound(nameof(_lb_max_tip), _lb_max_tip);
            EnsureBound(nameof(_lb_lock_tip), _lb_lock_tip);
            EnsureBound(nameof(_btn_levelup), _btn_levelup);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_group_item), _group_item);
            EnsureBound(nameof(_group_attr_cur), _group_attr_cur);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_lb_cur_attr), _lb_cur_attr);
            EnsureBound(nameof(_group_attr_next), _group_attr_next);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_lb_next_attr), _lb_next_attr);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_tx_desc1), _tx_desc1);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_tx_desc2), _tx_desc2);
            EnsureBound(nameof(_tx_desc3), _tx_desc3);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_fight_con), _fight_con);
            EnsureBound(nameof(_tpl_GuildSkillShowItem), _tpl_GuildSkillShowItem);
            EnsureBound(nameof(_tpl_FightingShowSmallItem), _tpl_FightingShowSmallItem);
        }
    }
}
