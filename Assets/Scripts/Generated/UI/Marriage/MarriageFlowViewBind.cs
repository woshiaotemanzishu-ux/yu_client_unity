// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageFlowView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageFlowViewBind : BaseView
    {
        public Image _Image1;
        public Image _btn_close;
        public Image _Image4;
        public ScrollRect _list;
        public GameObject _tpl_MarriageFlowItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_list), _list);
            EnsureBound(nameof(_tpl_MarriageFlowItem), _tpl_MarriageFlowItem);
        }
    }
}
