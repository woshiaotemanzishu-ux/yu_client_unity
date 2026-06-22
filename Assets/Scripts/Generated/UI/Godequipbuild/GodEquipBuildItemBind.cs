// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godequipbuild/GodEquipBuildItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Godequipbuild
{
    public partial class GodEquipBuildItemBind : BaseView
    {
        public RectTransform scaleGp;
        public Image icon_bg;
        public Image lock_icon;
        public Image _img_add;
        public RectTransform award_con;
        public RectTransform group_eff;
        public Image _img_select;
        public Image red_dot;
        public RectTransform gp_click;

        protected override void BindNodes()
        {
            EnsureBound(nameof(scaleGp), scaleGp);
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(lock_icon), lock_icon);
            EnsureBound(nameof(_img_add), _img_add);
            EnsureBound(nameof(award_con), award_con);
            EnsureBound(nameof(group_eff), group_eff);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(gp_click), gp_click);
        }
    }
}
