// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/flowerRank/FlowerRankItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FlowerRank
{
    public partial class FlowerRankItemBind : BaseView
    {
        public Image img_bg;
        public Image img_rank_icon;
        public RectTransform box_server_name;
        public TextMeshProUGUI lable_server_name;
        public TextMeshProUGUI lable_name;
        public Image img_flower_icon;
        public TextMeshProUGUI lable_flower_num;
        public TextMeshProUGUI lable_tips;
        public TextMeshProUGUI lable_rank;
        public RectTransform box_send_flower;
        public Image image_send_flower_btn;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_rank_icon), img_rank_icon);
            EnsureBound(nameof(box_server_name), box_server_name);
            EnsureBound(nameof(lable_server_name), lable_server_name);
            EnsureBound(nameof(lable_name), lable_name);
            EnsureBound(nameof(img_flower_icon), img_flower_icon);
            EnsureBound(nameof(lable_flower_num), lable_flower_num);
            EnsureBound(nameof(lable_tips), lable_tips);
            EnsureBound(nameof(lable_rank), lable_rank);
            EnsureBound(nameof(box_send_flower), box_send_flower);
            EnsureBound(nameof(image_send_flower_btn), image_send_flower_btn);
        }
    }
}
