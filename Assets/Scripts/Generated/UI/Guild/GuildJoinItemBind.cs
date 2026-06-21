// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildJoinItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildJoinItemBind : BaseView
    {
        public Image _img_bg;
        public Image _Image1;
        public RectTransform con1;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _lb_master;
        public RectTransform con2;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _lb_num;
        public RectTransform con3;
        public TextMeshProUGUI _Label3;
        public TextMeshProUGUI _lb_fight;
        public RectTransform _btn_join;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_cond;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(con1), con1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_lb_master), _lb_master);
            EnsureBound(nameof(con2), con2);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_lb_num), _lb_num);
            EnsureBound(nameof(con3), con3);
            EnsureBound(nameof(_Label3), _Label3);
            EnsureBound(nameof(_lb_fight), _lb_fight);
            EnsureBound(nameof(_btn_join), _btn_join);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_cond), _lb_cond);
        }
    }
}
