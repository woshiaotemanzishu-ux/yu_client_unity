// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageAskListView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageAskListViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image3;
        public Image _Image4;
        public ScrollRect _Scroller1;
        public ScrollRect _list;
        public Image _btn_close;
        public RectTransform _group_empty;
        public TextMeshProUGUI _Label1;
        public RectTransform _btn_go;
        public Image _Image5;
        public TextMeshProUGUI _Label2;
        public GameObject _tpl_MarriageAskListItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_list), _list);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_group_empty), _group_empty);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_tpl_MarriageAskListItem), _tpl_MarriageAskListItem);
        }
    }
}
