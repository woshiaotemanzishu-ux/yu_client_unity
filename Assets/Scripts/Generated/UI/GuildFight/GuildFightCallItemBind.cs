// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildFight/GuildFightCallItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GuildFight
{
    public partial class GuildFightCallItemBind : BaseView
    {
        public Image _bg;
        public TextMeshProUGUI guild_name;
        public Image call_high_img;
        public Image call_bg_img;
        public Image king_icon;
        public Image lock_img;
        public Image ord_icon;
        public Image titleImg;
        public Image blood_bg;
        public Image green_blood;
        public Image red_blood;
        public TextMeshProUGUI call_label;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(guild_name), guild_name);
            EnsureBound(nameof(call_high_img), call_high_img);
            EnsureBound(nameof(call_bg_img), call_bg_img);
            EnsureBound(nameof(king_icon), king_icon);
            EnsureBound(nameof(lock_img), lock_img);
            EnsureBound(nameof(ord_icon), ord_icon);
            EnsureBound(nameof(titleImg), titleImg);
            EnsureBound(nameof(blood_bg), blood_bg);
            EnsureBound(nameof(green_blood), green_blood);
            EnsureBound(nameof(red_blood), red_blood);
            EnsureBound(nameof(call_label), call_label);
        }
    }
}
