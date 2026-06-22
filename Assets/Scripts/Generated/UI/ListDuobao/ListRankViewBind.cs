// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/listDuobao/ListRankView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.ListDuobao
{
    public partial class ListRankViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_title;
        public Image _btn_single;
        public TextMeshProUGUI _lb_single;
        public Image _btn_all_;
        public TextMeshProUGUI _lb_all;
        public Image _btn_close;
        public Image _gp_title;
        public TextMeshProUGUI _lb_title;
        public ScrollRect _player_rank;
        public ScrollRect _server_rank;
        public ScrollRect _gp_reward;
        public Image _img_rank;
        public TextMeshProUGUI _lb_rank;
        public TextMeshProUGUI _lb_myrank;
        public TextMeshProUGUI _lb_myscore;
        public TextMeshProUGUI _lb_tips;
        public GameObject _tpl_ListRankItem;
        public GameObject _tpl_ListGoodsItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_btn_single), _btn_single);
            EnsureBound(nameof(_lb_single), _lb_single);
            EnsureBound(nameof(_btn_all_), _btn_all_);
            EnsureBound(nameof(_lb_all), _lb_all);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_gp_title), _gp_title);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_player_rank), _player_rank);
            EnsureBound(nameof(_server_rank), _server_rank);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_img_rank), _img_rank);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_lb_myrank), _lb_myrank);
            EnsureBound(nameof(_lb_myscore), _lb_myscore);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_tpl_ListRankItem), _tpl_ListRankItem);
            EnsureBound(nameof(_tpl_ListGoodsItem), _tpl_ListGoodsItem);
        }
    }
}
