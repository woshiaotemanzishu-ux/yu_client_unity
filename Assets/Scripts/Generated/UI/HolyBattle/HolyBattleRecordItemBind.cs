// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyBattle/HolyBattleRecordItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyBattle
{
    public partial class HolyBattleRecordItemBind : BaseView
    {
        public Image _img_bg;
        public Image img_rank;
        public TextMeshProUGUI _lb_rank;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_score;
        public TextMeshProUGUI _lb_count;
        public Image _img_line;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(img_rank), img_rank);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_score), _lb_score);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_img_line), _img_line);
        }
    }
}
