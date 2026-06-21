// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildDepotSelectView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildDepotSelectViewBind : BaseView
    {
        public Image _btn_close;
        public Image btnDonate;
        public TextMeshProUGUI equipScore;
        public ScrollRect _list;
        public GameObject _tpl_GuildDepotItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(btnDonate), btnDonate);
            EnsureBound(nameof(equipScore), equipScore);
            EnsureBound(nameof(_list), _list);
            EnsureBound(nameof(_tpl_GuildDepotItem), _tpl_GuildDepotItem);
        }
    }
}
