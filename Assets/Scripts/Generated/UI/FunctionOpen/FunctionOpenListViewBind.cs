// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/functionOpen/FunctionOpenListView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FunctionOpen
{
    public partial class FunctionOpenListViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image3;
        public Image _Image2;
        public Image _Image4;
        public Image close_btn;
        public ScrollRect Content;
        public GameObject _tpl_FunctionOpenListItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(close_btn), close_btn);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_FunctionOpenListItem), _tpl_FunctionOpenListItem);
        }
    }
}
