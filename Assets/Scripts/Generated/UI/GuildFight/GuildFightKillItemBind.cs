// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildFight/GuildFightKillItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GuildFight
{
    public partial class GuildFightKillItemBind : BaseView
    {
        public RectTransform kill_tips_group;
        public Image kill_bg;
        public Image kill_avatar_icon;
        public Image kill_image;
        public RectTransform kill_head;
        public RectTransform _box_name;
        public Image kill_vip_flag;
        public TextMeshProUGUI kill_name;
        public TextMeshProUGUI kill_number;
        public Image _Image1;

        protected override void BindNodes()
        {
            EnsureBound(nameof(kill_tips_group), kill_tips_group);
            EnsureBound(nameof(kill_bg), kill_bg);
            EnsureBound(nameof(kill_avatar_icon), kill_avatar_icon);
            EnsureBound(nameof(kill_image), kill_image);
            EnsureBound(nameof(kill_head), kill_head);
            EnsureBound(nameof(_box_name), _box_name);
            EnsureBound(nameof(kill_vip_flag), kill_vip_flag);
            EnsureBound(nameof(kill_name), kill_name);
            EnsureBound(nameof(kill_number), kill_number);
            EnsureBound(nameof(_Image1), _Image1);
        }
    }
}
