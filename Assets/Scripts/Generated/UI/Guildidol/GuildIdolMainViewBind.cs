// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildidol/GuildIdolMainView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guildidol
{
    public partial class GuildIdolMainViewBind : BaseView
    {
        public RectTransform _gp_view;
        public ScrollRect _tab_list;
        public ScrollRect _item_list;
        public GameObject _tpl_GuildIdolMainItem;
        public GameObject _tpl_GuildIdolMainTab;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_view), _gp_view);
            EnsureBound(nameof(_tab_list), _tab_list);
            EnsureBound(nameof(_item_list), _item_list);
            EnsureBound(nameof(_tpl_GuildIdolMainItem), _tpl_GuildIdolMainItem);
            EnsureBound(nameof(_tpl_GuildIdolMainTab), _tpl_GuildIdolMainTab);
        }
    }
}
