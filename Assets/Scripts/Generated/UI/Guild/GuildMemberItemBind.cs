// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildMemberItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildMemberItemBind : BaseView
    {
        public RectTransform _click_group;
        public Image _img_bg;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _lb_fight;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _lb_time;
        public TextMeshProUGUI _lb_title;
        public RectTransform _Group1;
        public Image _img_pos;
        public TextMeshProUGUI _lb_name;
        public RectTransform _playerHead;
        public Image _click_bg;
        public RectTransform _btn_out;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_click_group), _click_group);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_lb_fight), _lb_fight);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_img_pos), _img_pos);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_playerHead), _playerHead);
            EnsureBound(nameof(_click_bg), _click_bg);
            EnsureBound(nameof(_btn_out), _btn_out);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
        }
    }
}
