// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnQuizHistoryItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnQuizHistoryItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI stage_txt;
        public TextMeshProUGUI cost_num;
        public TextMeshProUGUI zhanhun;
        public TextMeshProUGUI role_name;
        public TextMeshProUGUI sever_name;
        public Image result;
        public RectTransform cost_icon;
        public RectTransform get_icon;
        public RectTransform getBtn;
        public Image btn_img;
        public TextMeshProUGUI get_label;
        public Image red_dot;
        public Image get_state;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(stage_txt), stage_txt);
            EnsureBound(nameof(cost_num), cost_num);
            EnsureBound(nameof(zhanhun), zhanhun);
            EnsureBound(nameof(role_name), role_name);
            EnsureBound(nameof(sever_name), sever_name);
            EnsureBound(nameof(result), result);
            EnsureBound(nameof(cost_icon), cost_icon);
            EnsureBound(nameof(get_icon), get_icon);
            EnsureBound(nameof(getBtn), getBtn);
            EnsureBound(nameof(btn_img), btn_img);
            EnsureBound(nameof(get_label), get_label);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(get_state), get_state);
        }
    }
}
