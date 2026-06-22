// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/boss/BossWayTaskItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Boss
{
    public partial class BossWayTaskItemBind : BaseView
    {
        public RectTransform clickGp;
        public Image _Image1;
        public Image _Image2;
        public Image getImg;
        public TextMeshProUGUI taskLb;
        public TextMeshProUGUI desLb;
        public TextMeshProUGUI getLb;
        public Image selectImg;
        public Image allBg;
        public TextMeshProUGUI allFinish;

        protected override void BindNodes()
        {
            EnsureBound(nameof(clickGp), clickGp);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(getImg), getImg);
            EnsureBound(nameof(taskLb), taskLb);
            EnsureBound(nameof(desLb), desLb);
            EnsureBound(nameof(getLb), getLb);
            EnsureBound(nameof(selectImg), selectImg);
            EnsureBound(nameof(allBg), allBg);
            EnsureBound(nameof(allFinish), allFinish);
        }
    }
}
