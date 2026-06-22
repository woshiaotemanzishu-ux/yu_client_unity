// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/boss/BossTargetItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Boss
{
    public partial class BossTargetItemBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _list_item;
        public GameObject _tpl_BossTargetSubItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_tpl_BossTargetSubItem), _tpl_BossTargetSubItem);
        }
    }
}
