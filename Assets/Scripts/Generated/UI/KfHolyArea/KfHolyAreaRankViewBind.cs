// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaRankView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaRankViewBind : BaseView
    {
        public Image bg_img;
        public TextMeshProUGUI title_lb;
        public TextMeshProUGUI server_name;
        public RectTransform _gp_box;
        public RectTransform _gp_behind;
        public Image _img_box;
        public RectTransform _gp_front;
        public RectTransform _Group2;
        public RectTransform _gp_model;
        public Image img_rank;
        public TextMeshProUGUI name_lb;
        public ScrollRect Content;
        public TextMeshProUGUI rank_lb;
        public TextMeshProUGUI score_lb;
        public RectTransform _gp_close;
        public Image _img_tips;
        public GameObject _tpl_KfHolyAreaRankItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg_img), bg_img);
            EnsureBound(nameof(title_lb), title_lb);
            EnsureBound(nameof(server_name), server_name);
            EnsureBound(nameof(_gp_box), _gp_box);
            EnsureBound(nameof(_gp_behind), _gp_behind);
            EnsureBound(nameof(_img_box), _img_box);
            EnsureBound(nameof(_gp_front), _gp_front);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_gp_model), _gp_model);
            EnsureBound(nameof(img_rank), img_rank);
            EnsureBound(nameof(name_lb), name_lb);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(rank_lb), rank_lb);
            EnsureBound(nameof(score_lb), score_lb);
            EnsureBound(nameof(_gp_close), _gp_close);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_tpl_KfHolyAreaRankItem), _tpl_KfHolyAreaRankItem);
        }
    }
}
