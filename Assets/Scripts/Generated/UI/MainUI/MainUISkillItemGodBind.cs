// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUISkillItemGod.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUISkillItemGodBind : BaseView
    {
        public RectTransform con;
        public Image bg;
        public Image icon;
        public Image _Image1;
        public Image _img_keep;
        public Image _img_mask;
        public TextMeshProUGUI _lb_cd;
        public RectTransform _gp_eff;

        protected override void BindNodes()
        {
            EnsureBound(nameof(con), con);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_keep), _img_keep);
            EnsureBound(nameof(_img_mask), _img_mask);
            EnsureBound(nameof(_lb_cd), _lb_cd);
            EnsureBound(nameof(_gp_eff), _gp_eff);
        }
    }
}
