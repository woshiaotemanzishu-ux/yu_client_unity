// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaBuildMsgBossItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaBuildMsgBossItemBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _lb_left_time;
        public TextMeshProUGUI _lb_boss_name;
        public TextMeshProUGUI _lb_level;
        public Image _img_boss_head;
        public Image _img_select;
        public RectTransform _gp_peace;
        public Image _Image3;
        public TextMeshProUGUI _Label1;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_left_time), _lb_left_time);
            EnsureBound(nameof(_lb_boss_name), _lb_boss_name);
            EnsureBound(nameof(_lb_level), _lb_level);
            EnsureBound(nameof(_img_boss_head), _img_boss_head);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_gp_peace), _gp_peace);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Label1), _Label1);
        }
    }
}
