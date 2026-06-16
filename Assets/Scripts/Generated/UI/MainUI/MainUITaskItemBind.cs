// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUITaskItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUITaskItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_done;
        public Image _img_select;
        public TextMeshProUGUI lblTaskTitle;
        public TextMeshProUGUI lblTaskTitle2;
        public RectTransform _box_finger_con;
        public RectTransform _box_effect;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_done), _img_done);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(lblTaskTitle), lblTaskTitle);
            EnsureBound(nameof(lblTaskTitle2), lblTaskTitle2);
            EnsureBound(nameof(_box_finger_con), _box_finger_con);
            EnsureBound(nameof(_box_effect), _box_effect);
        }
    }
}
