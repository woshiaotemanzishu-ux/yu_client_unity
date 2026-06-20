// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/rune/RuneSkillItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Rune
{
    public partial class RuneSkillItemBind : BaseView
    {
        public RectTransform root_wnd;
        public RectTransform garyGp;
        public Image mg0;
        public Image skillImg;
        public Image SelectImg;
        public Image red;
        public Image lockImg;
        public Image lockImg2;
        public Image activeImg;
        public TextMeshProUGUI lockLb;
        public RectTransform effectGp1;
        public RectTransform effectGp;

        protected override void BindNodes()
        {
            EnsureBound(nameof(root_wnd), root_wnd);
            EnsureBound(nameof(garyGp), garyGp);
            EnsureBound(nameof(mg0), mg0);
            EnsureBound(nameof(skillImg), skillImg);
            EnsureBound(nameof(SelectImg), SelectImg);
            EnsureBound(nameof(red), red);
            EnsureBound(nameof(lockImg), lockImg);
            EnsureBound(nameof(lockImg2), lockImg2);
            EnsureBound(nameof(activeImg), activeImg);
            EnsureBound(nameof(lockLb), lockLb);
            EnsureBound(nameof(effectGp1), effectGp1);
            EnsureBound(nameof(effectGp), effectGp);
        }
    }
}
