// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equip/EquipSmeltItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Equip
{
    public partial class EquipSmeltItemBind : BaseView
    {
        public Image icon_bg;
        public Image lock_icon;
        public RectTransform award_con;
        public Image select_img;
        public TextMeshProUGUI level;
        public TextMeshProUGUI tips;
        public TextMeshProUGUI grade;
        public Image red_dot;
        public RectTransform group_eff;
        public Image click_bg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(lock_icon), lock_icon);
            EnsureBound(nameof(award_con), award_con);
            EnsureBound(nameof(select_img), select_img);
            EnsureBound(nameof(level), level);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(grade), grade);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(group_eff), group_eff);
            EnsureBound(nameof(click_bg), click_bg);
        }
    }
}
