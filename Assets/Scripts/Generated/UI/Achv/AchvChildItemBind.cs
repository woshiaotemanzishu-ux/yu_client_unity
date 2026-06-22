// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/achv/AchvChildItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Achv
{
    public partial class AchvChildItemBind : BaseView
    {
        public Image typeImg;
        public TextMeshProUGUI nameLb;
        public RectTransform _box_pg;
        public Image _Image1;
        public TextMeshProUGUI scheduleLb;
        public ScrollRect _pg_panal;
        public Image scheduleImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(typeImg), typeImg);
            EnsureBound(nameof(nameLb), nameLb);
            EnsureBound(nameof(_box_pg), _box_pg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(scheduleLb), scheduleLb);
            EnsureBound(nameof(_pg_panal), _pg_panal);
            EnsureBound(nameof(scheduleImg), scheduleImg);
        }
    }
}
