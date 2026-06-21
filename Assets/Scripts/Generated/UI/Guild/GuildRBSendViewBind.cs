// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildRBSendView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildRBSendViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _btn_close;
        public ScrollRect _list_items;
        public GameObject _tpl_GuildRBSendItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_list_items), _list_items);
            EnsureBound(nameof(_tpl_GuildRBSendItem), _tpl_GuildRBSendItem);
        }
    }
}
