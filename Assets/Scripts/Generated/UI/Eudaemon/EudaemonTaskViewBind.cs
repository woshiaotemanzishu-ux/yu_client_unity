// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eudaemon/EudaemonTaskView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eudaemon
{
    public partial class EudaemonTaskViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_bg3;
        public Image _img_bg4;
        public Image _img_close;
        public Image _img_tips;
        public RectTransform _box_item;
        public RectTransform _box_tab;
        public GameObject _tpl_EudaemonTaskTabItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_img_bg4), _img_bg4);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_box_item), _box_item);
            EnsureBound(nameof(_box_tab), _box_tab);
            EnsureBound(nameof(_tpl_EudaemonTaskTabItem), _tpl_EudaemonTaskTabItem);
        }
    }
}
