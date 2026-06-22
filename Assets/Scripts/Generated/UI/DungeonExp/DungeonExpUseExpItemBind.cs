// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonExp/DungeonExpUseExpItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonExp
{
    public partial class DungeonExpUseExpItemBind : BaseView
    {
        public Image _img_bg;
        public Image _title_bg;
        public Image tip;
        public TextMeshProUGUI _lb_exp_name;
        public TextMeshProUGUI _lb_use_tips;
        public TextMeshProUGUI _lb_count_down;
        public RectTransform _box_award;
        public RectTransform _box_use;
        public Image _img_use;
        public TextMeshProUGUI _lb_use;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_title_bg), _title_bg);
            EnsureBound(nameof(tip), tip);
            EnsureBound(nameof(_lb_exp_name), _lb_exp_name);
            EnsureBound(nameof(_lb_use_tips), _lb_use_tips);
            EnsureBound(nameof(_lb_count_down), _lb_count_down);
            EnsureBound(nameof(_box_award), _box_award);
            EnsureBound(nameof(_box_use), _box_use);
            EnsureBound(nameof(_img_use), _img_use);
            EnsureBound(nameof(_lb_use), _lb_use);
        }
    }
}
