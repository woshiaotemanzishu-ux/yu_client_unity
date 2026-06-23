// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/task/TaskUpAlertView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Task
{
    public partial class TaskUpAlertViewBind : BaseView
    {
        public Image _img_bg;
        public Image _Image1;
        public TextMeshProUGUI _Label1;
        public ScrollRect _scr_item;
        public RectTransform Content;
        public RectTransform _btn_sure;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public Image _img_close;
        public Image banner;
        public GameObject _tpl_TaskUpAlertItem;
        public GameObject _tpl_TaskUpAlerStartItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_scr_item), _scr_item);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_btn_sure), _btn_sure);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(banner), banner);
            EnsureBound(nameof(_tpl_TaskUpAlertItem), _tpl_TaskUpAlertItem);
            EnsureBound(nameof(_tpl_TaskUpAlerStartItem), _tpl_TaskUpAlerStartItem);
        }
    }
}
