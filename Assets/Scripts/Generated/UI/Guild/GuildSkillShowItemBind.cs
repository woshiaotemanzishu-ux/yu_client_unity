// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildSkillShowItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildSkillShowItemBind : BaseView
    {
        public Image _Image1;
        public Image _img_icon;
        public Image _img_lock;
        public Image _img_select;
        public RectTransform _group_lv;
        public Image _Image2;
        public TextMeshProUGUI _lb_level;
        public Image _Image3;
        public TextMeshProUGUI _lb_name;
        public Image _reddot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_lock), _img_lock);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_group_lv), _group_lv);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_level), _lb_level);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_reddot), _reddot);
        }
    }
}
