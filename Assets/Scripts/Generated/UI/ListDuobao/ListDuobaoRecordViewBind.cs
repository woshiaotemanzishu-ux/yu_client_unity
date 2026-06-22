// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/listDuobao/ListDuobaoRecordView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.ListDuobao
{
    public partial class ListDuobaoRecordViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_title;
        public TextMeshProUGUI _lb_title;
        public Image _img_close;
        public RectTransform _gp_record;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_gp_record), _gp_record);
        }
    }
}
