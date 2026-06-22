// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossMystery/BossMysteryMonItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossMystery
{
    public partial class BossMysteryMonItemBind : BaseView
    {
        public Image _img_bg;
        public Image circle_image;
        public Image mon_icon;
        public Image step_bg;
        public TextMeshProUGUI step_num;
        public TextMeshProUGUI _lb_unopen_tips;
        public Image _img_dead;
        public RectTransform refresh_con;
        public Image _Image3;
        public TextMeshProUGUI refresh_time;
        public TextMeshProUGUI _lb_alive;
        public Image _img_select;
        public RectTransform reborn_effect;
        public Image _img_special;
        public RectTransform _Group2;
        public TextMeshProUGUI _lb_bossName;
        public TextMeshProUGUI _lb_level;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(circle_image), circle_image);
            EnsureBound(nameof(mon_icon), mon_icon);
            EnsureBound(nameof(step_bg), step_bg);
            EnsureBound(nameof(step_num), step_num);
            EnsureBound(nameof(_lb_unopen_tips), _lb_unopen_tips);
            EnsureBound(nameof(_img_dead), _img_dead);
            EnsureBound(nameof(refresh_con), refresh_con);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(refresh_time), refresh_time);
            EnsureBound(nameof(_lb_alive), _lb_alive);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(reborn_effect), reborn_effect);
            EnsureBound(nameof(_img_special), _img_special);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_lb_bossName), _lb_bossName);
            EnsureBound(nameof(_lb_level), _lb_level);
        }
    }
}
