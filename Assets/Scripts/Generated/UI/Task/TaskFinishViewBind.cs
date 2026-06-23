// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/task/TaskFinishView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Task
{
    public partial class TaskFinishViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_title;
        public RectTransform finifsh_effect;
        public Image _img_close;
        public Image _img_bg3;
        public TextMeshProUGUI _lb_content;
        public TextMeshProUGUI _lb_title2;
        public Image icon_tag;
        public Image _img_bg4;
        public TextMeshProUGUI _lb_desc;
        public TextMeshProUGUI _lb_task_target;
        public TextMeshProUGUI _html_task_target;
        public RectTransform _box_reward;
        public TextMeshProUGUI _lb_desc2;
        public ScrollRect _panel_reward;
        public RectTransform _hbox_reward;
        public RectTransform _box_finish;
        public Image _img_finish;
        public TextMeshProUGUI _lb_finish;
        public TextMeshProUGUI _lb_count_down;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(finifsh_effect), finifsh_effect);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_lb_content), _lb_content);
            EnsureBound(nameof(_lb_title2), _lb_title2);
            EnsureBound(nameof(icon_tag), icon_tag);
            EnsureBound(nameof(_img_bg4), _img_bg4);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_lb_task_target), _lb_task_target);
            EnsureBound(nameof(_html_task_target), _html_task_target);
            EnsureBound(nameof(_box_reward), _box_reward);
            EnsureBound(nameof(_lb_desc2), _lb_desc2);
            EnsureBound(nameof(_panel_reward), _panel_reward);
            EnsureBound(nameof(_hbox_reward), _hbox_reward);
            EnsureBound(nameof(_box_finish), _box_finish);
            EnsureBound(nameof(_img_finish), _img_finish);
            EnsureBound(nameof(_lb_finish), _lb_finish);
            EnsureBound(nameof(_lb_count_down), _lb_count_down);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
