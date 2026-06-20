// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/FriendApllyPopItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class FriendApllyPopItemBind : BaseView
    {
        public Image _Image1;
        public Image _vip_icon;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_level;
        public TextMeshProUGUI _lb_fight;
        public RectTransform btn_no;
        public RectTransform btn_yes;
        public Image _Image2;
        public RectTransform head;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_vip_icon), _vip_icon);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_level), _lb_level);
            EnsureBound(nameof(_lb_fight), _lb_fight);
            EnsureBound(nameof(btn_no), btn_no);
            EnsureBound(nameof(btn_yes), btn_yes);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(head), head);
        }
    }
}
