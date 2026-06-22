// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/luckyDuobao/LuckyDuoBaoLimitBuyView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LuckyDuobao
{
    public partial class LuckyDuoBaoLimitBuyViewBind : BaseView
    {
        public Image _img_title;
        public RectTransform _gp_time;
        public RectTransform _gp_sp;
        public Image _img_sp;
        public RectTransform _btn_get;
        public Image _img_btn;
        public TextMeshProUGUI _lb_btn;
        public ScrollRect sv;
        public RectTransform Content;
        public GameObject _tpl_LuckyDuoBaoLimitBuyItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(_gp_sp), _gp_sp);
            EnsureBound(nameof(_img_sp), _img_sp);
            EnsureBound(nameof(_btn_get), _btn_get);
            EnsureBound(nameof(_img_btn), _img_btn);
            EnsureBound(nameof(_lb_btn), _lb_btn);
            EnsureBound(nameof(sv), sv);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_LuckyDuoBaoLimitBuyItem), _tpl_LuckyDuoBaoLimitBuyItem);
        }
    }
}
