// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/competelist/CompetelistRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Competelist
{
    public partial class CompetelistRewardViewBind : BaseView
    {
        public Image _btn_close;
        public ScrollRect _Scroller1;
        public GameObject _tpl_CompetelistRewaedItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_tpl_CompetelistRewaedItem), _tpl_CompetelistRewaedItem);
        }
    }
}
