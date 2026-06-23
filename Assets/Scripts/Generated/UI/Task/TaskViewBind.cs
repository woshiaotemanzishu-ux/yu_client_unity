// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/task/TaskView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Task
{
    public partial class TaskViewBind : BaseView
    {
        public Image bg;
        public Image image1;
        public Image closeBtn;
        public ScrollRect scroll;
        public RectTransform Content;
        public GameObject _tpl_TaskBarItem;
        public GameObject _tpl_TaskContentSubView;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(image1), image1);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_TaskBarItem), _tpl_TaskBarItem);
            EnsureBound(nameof(_tpl_TaskContentSubView), _tpl_TaskContentSubView);
        }
    }
}
