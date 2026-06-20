// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/rune/RuneDecMainView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Rune
{
    public partial class RuneDecMainViewBind : BaseView
    {
        public Image _bg;
        public RectTransform _Group1;
        public Image _Image1;
        public TextMeshProUGUI _img_title;
        public ScrollRect Content;
        public RectTransform sub_group;
        public Image _btn_close;
        public RectTransform _gp_effect;
        public GameObject _tpl_RuneDecomposeTab;
        public GameObject _tpl_RuneDecomposeView;
        public GameObject _tpl_RuneDecomposeItem;
        public GameObject _tpl_RuneResolutionView;
        public GameObject _tpl_RuneResolutionItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(sub_group), sub_group);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(_tpl_RuneDecomposeTab), _tpl_RuneDecomposeTab);
            EnsureBound(nameof(_tpl_RuneDecomposeView), _tpl_RuneDecomposeView);
            EnsureBound(nameof(_tpl_RuneDecomposeItem), _tpl_RuneDecomposeItem);
            EnsureBound(nameof(_tpl_RuneResolutionView), _tpl_RuneResolutionView);
            EnsureBound(nameof(_tpl_RuneResolutionItem), _tpl_RuneResolutionItem);
        }
    }
}
