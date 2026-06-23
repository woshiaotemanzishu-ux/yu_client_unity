// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/task/TaskAutoSettingView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Task
{
    public partial class TaskAutoSettingViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_title;
        public TextMeshProUGUI _lb_desc;
        public TextMeshProUGUI _lb_count_down;
        public Image _img_close;
        public Image _img_confirm;
        public TextMeshProUGUI _lb_ok;
        public GameObject _tpl_TheCheckBox;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_lb_count_down), _lb_count_down);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_confirm), _img_confirm);
            EnsureBound(nameof(_lb_ok), _lb_ok);
            EnsureBound(nameof(_tpl_TheCheckBox), _tpl_TheCheckBox);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
