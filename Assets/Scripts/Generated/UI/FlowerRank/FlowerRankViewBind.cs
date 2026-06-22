// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/flowerRank/FlowerRankView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FlowerRank
{
    public partial class FlowerRankViewBind : BaseView
    {
        public Image img_top_rank_bg;
        public RectTransform box_top_item_group;
        public TextMeshProUGUI html_time_1;
        public TextMeshProUGUI html_time_2;
        public Image img_reward_btn;
        public Image item_bg;
        public ScrollRect list_flower_item_group;
        public RectTransform box_bottom;
        public TextMeshProUGUI lable_bottom_rank;
        public Image img_flower_rank;
        public TextMeshProUGUI lable_bottom_flower_num;
        public TextMeshProUGUI lable_disparity_flower_num;
        public Image img_btn_recive_flower;
        public TextMeshProUGUI lable_bottom_lable_tips;
        public GameObject _tpl_FlowerRankItem;
        public GameObject _tpl_FlowerTopRankItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_top_rank_bg), img_top_rank_bg);
            EnsureBound(nameof(box_top_item_group), box_top_item_group);
            EnsureBound(nameof(html_time_1), html_time_1);
            EnsureBound(nameof(html_time_2), html_time_2);
            EnsureBound(nameof(img_reward_btn), img_reward_btn);
            EnsureBound(nameof(item_bg), item_bg);
            EnsureBound(nameof(list_flower_item_group), list_flower_item_group);
            EnsureBound(nameof(box_bottom), box_bottom);
            EnsureBound(nameof(lable_bottom_rank), lable_bottom_rank);
            EnsureBound(nameof(img_flower_rank), img_flower_rank);
            EnsureBound(nameof(lable_bottom_flower_num), lable_bottom_flower_num);
            EnsureBound(nameof(lable_disparity_flower_num), lable_disparity_flower_num);
            EnsureBound(nameof(img_btn_recive_flower), img_btn_recive_flower);
            EnsureBound(nameof(lable_bottom_lable_tips), lable_bottom_lable_tips);
            EnsureBound(nameof(_tpl_FlowerRankItem), _tpl_FlowerRankItem);
            EnsureBound(nameof(_tpl_FlowerTopRankItem), _tpl_FlowerTopRankItem);
        }
    }
}
