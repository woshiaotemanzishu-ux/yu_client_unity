// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarReliveView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarReliveViewBind : BaseView
    {
        public RectTransform _Group2;
        public Image _img_bg;
        public Image _img_bg2;
        public RectTransform _gp_title;
        public Image _Image4;
        public TextMeshProUGUI _bm_relive_time;
        public TextMeshProUGUI _lb_left_count;
        public TextMeshProUGUI _lb_des2;
        public TextMeshProUGUI _lb_des;
        public Image _img_mon_head;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_gp_title), _gp_title);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_bm_relive_time), _bm_relive_time);
            EnsureBound(nameof(_lb_left_count), _lb_left_count);
            EnsureBound(nameof(_lb_des2), _lb_des2);
            EnsureBound(nameof(_lb_des), _lb_des);
            EnsureBound(nameof(_img_mon_head), _img_mon_head);
        }
    }
}
