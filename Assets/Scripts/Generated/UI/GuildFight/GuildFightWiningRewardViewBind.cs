// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildFight/GuildFightWiningRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GuildFight
{
    public partial class GuildFightWiningRewardViewBind : BaseView
    {
        public Image _Image1;
        public Image _title_Image2;
        public TextMeshProUGUI _lb_win_name;
        public Image CloseBtn;
        public Image _Image2;
        public TextMeshProUGUI num_text;
        public ScrollRect _list_items;
        public GameObject _tpl_GuildFightWiningRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_title_Image2), _title_Image2);
            EnsureBound(nameof(_lb_win_name), _lb_win_name);
            EnsureBound(nameof(CloseBtn), CloseBtn);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(num_text), num_text);
            EnsureBound(nameof(_list_items), _list_items);
            EnsureBound(nameof(_tpl_GuildFightWiningRewardItem), _tpl_GuildFightWiningRewardItem);
        }
    }
}
