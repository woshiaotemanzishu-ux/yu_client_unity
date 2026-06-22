// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarRankItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarRankItemBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _lb_rank;
        public RectTransform _gp;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_time;
        public ScrollRect Content;
        public TextMeshProUGUI _lb_null;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_gp), _gp);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_lb_null), _lb_null);
        }
    }
}
