// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildidol/GuildIdolRuneDecItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guildidol
{
    public partial class GuildIdolRuneDecItemBind : BaseView
    {
        public RectTransform _gp;
        public Image _select;
        public RectTransform _box;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp), _gp);
            EnsureBound(nameof(_select), _select);
            EnsureBound(nameof(_box), _box);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
