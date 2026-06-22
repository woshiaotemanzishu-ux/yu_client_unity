// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/flowerRank/FlowerTopRankItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FlowerRank
{
    public partial class FlowerTopRankItemBind : BaseView
    {
        public RectTransform box_top_effect;
        public Image img_top_honor;
        public TextMeshProUGUI lable_name;
        public Image img_foot_effect;
        public RectTransform box_role;
        public RectTransform box_send_flower;
        public Image img_send_flower_bg;
        public Image img_send_flower_btn;
        public TextMeshProUGUI lable_flower_num;
        public RectTransform box_middle_tips;
        public TextMeshProUGUI lable_middle_flower_num;

        protected override void BindNodes()
        {
            EnsureBound(nameof(box_top_effect), box_top_effect);
            EnsureBound(nameof(img_top_honor), img_top_honor);
            EnsureBound(nameof(lable_name), lable_name);
            EnsureBound(nameof(img_foot_effect), img_foot_effect);
            EnsureBound(nameof(box_role), box_role);
            EnsureBound(nameof(box_send_flower), box_send_flower);
            EnsureBound(nameof(img_send_flower_bg), img_send_flower_bg);
            EnsureBound(nameof(img_send_flower_btn), img_send_flower_btn);
            EnsureBound(nameof(lable_flower_num), lable_flower_num);
            EnsureBound(nameof(box_middle_tips), box_middle_tips);
            EnsureBound(nameof(lable_middle_flower_num), lable_middle_flower_num);
        }
    }
}
