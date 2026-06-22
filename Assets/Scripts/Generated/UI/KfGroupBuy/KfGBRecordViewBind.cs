// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfGroupBuy/KfGBRecordView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfGroupBuy
{
    public partial class KfGBRecordViewBind : BaseView
    {
        public Image _Image1;
        public RectTransform _Group1;
        public Image _Image3;
        public TextMeshProUGUI _Label1;
        public ScrollRect _scr_log;
        public ScrollRect _gp_data;
        public Image _img_close;
        public GameObject _tpl_KfGBRecordItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_scr_log), _scr_log);
            EnsureBound(nameof(_gp_data), _gp_data);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_tpl_KfGBRecordItem), _tpl_KfGBRecordItem);
        }
    }
}
