// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/task/TaskBarItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Task
{
    public partial class TaskBarItemBind : BaseView
    {
        public RectTransform conta;
        public RectTransform tab;
        public Image btn_img;
        public Image img2;
        public TextMeshProUGUI tab_txt;
        public Image arrow;
        public Image red_dot;
        public RectTransform subCon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(conta), conta);
            EnsureBound(nameof(tab), tab);
            EnsureBound(nameof(btn_img), btn_img);
            EnsureBound(nameof(img2), img2);
            EnsureBound(nameof(tab_txt), tab_txt);
            EnsureBound(nameof(arrow), arrow);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(subCon), subCon);
        }
    }
}
