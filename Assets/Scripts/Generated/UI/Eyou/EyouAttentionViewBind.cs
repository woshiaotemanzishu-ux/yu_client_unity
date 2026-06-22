// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eyou/EyouAttentionView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eyou
{
    public partial class EyouAttentionViewBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _list_item;
        public GameObject _tpl_EyouAttentionItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_tpl_EyouAttentionItem), _tpl_EyouAttentionItem);
        }
    }
}
