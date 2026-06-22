// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningIntroView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningIntroViewBind : BaseView
    {
        public Image img_bg;
        public Image _Image3;
        public RectTransform _Group1;
        public ScrollRect _Scroller1;
        public RectTransform _group_data;
        public RectTransform _btn_close;
        public Image _Image1;
        public ScrollRect _list_btn;
        public Image img_line;
        public Image _Image2_title;
        public TextMeshProUGUI _lb_win_name;
        public GameObject _tpl_EveningIntroItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_group_data), _group_data);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_list_btn), _list_btn);
            EnsureBound(nameof(img_line), img_line);
            EnsureBound(nameof(_Image2_title), _Image2_title);
            EnsureBound(nameof(_lb_win_name), _lb_win_name);
            EnsureBound(nameof(_tpl_EveningIntroItem), _tpl_EveningIntroItem);
        }
    }
}
