// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonMainLine/DungeonMainLineFightSceneItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonMainLine
{
    public partial class DungeonMainLineFightSceneItemBind : BaseView
    {
        public RectTransform _gp_con;
        public Image _img_bg;
        public TextMeshProUGUI _lb_kill_desc;
        public Image _Image3;
        public Image _Image4;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _Label3;
        public RectTransform arrow_box;
        public Image _img_arrow;
        public Image _skill;
        public ScrollRect _gp_reward;
        public RectTransform con;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_kill_desc), _lb_kill_desc);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_Label3), _Label3);
            EnsureBound(nameof(arrow_box), arrow_box);
            EnsureBound(nameof(_img_arrow), _img_arrow);
            EnsureBound(nameof(_skill), _skill);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(con), con);
        }
    }
}
