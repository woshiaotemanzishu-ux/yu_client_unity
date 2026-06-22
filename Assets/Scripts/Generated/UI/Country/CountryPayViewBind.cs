// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/country/CountryPayView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Country
{
    public partial class CountryPayViewBind : BaseView
    {
        public Image bg;
        public Image closeBtn;
        public Image _Image2;
        public Image _Image4;
        public ScrollRect Content;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _Label3;
        public Image _Image2_title;
        public TextMeshProUGUI _lb_win_name;
        public GameObject _tpl_CountryPayItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_Label3), _Label3);
            EnsureBound(nameof(_Image2_title), _Image2_title);
            EnsureBound(nameof(_lb_win_name), _lb_win_name);
            EnsureBound(nameof(_tpl_CountryPayItem), _tpl_CountryPayItem);
        }
    }
}
