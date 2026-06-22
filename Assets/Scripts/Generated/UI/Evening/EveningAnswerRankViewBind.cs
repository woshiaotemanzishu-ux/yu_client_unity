// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningAnswerRankView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningAnswerRankViewBind : BaseView
    {
        public RectTransform _box_rank;
        public Image _rank_box_bg;
        public Image _img6;
        public Image _img_jiejie_b;
        public TextMeshProUGUI _jie_txt;
        public ScrollRect _lits_ranks;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _lb_rank;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _lb_score;
        public Image _btn_award;
        public TextMeshProUGUI _lb_rank_tips;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_rank), _box_rank);
            EnsureBound(nameof(_rank_box_bg), _rank_box_bg);
            EnsureBound(nameof(_img6), _img6);
            EnsureBound(nameof(_img_jiejie_b), _img_jiejie_b);
            EnsureBound(nameof(_jie_txt), _jie_txt);
            EnsureBound(nameof(_lits_ranks), _lits_ranks);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_lb_score), _lb_score);
            EnsureBound(nameof(_btn_award), _btn_award);
            EnsureBound(nameof(_lb_rank_tips), _lb_rank_tips);
        }
    }
}
