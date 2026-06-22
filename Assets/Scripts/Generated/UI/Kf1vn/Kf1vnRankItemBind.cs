// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnRankItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnRankItemBind : BaseView
    {
        public Image bg_img;
        public Image rank_icon;
        public TextMeshProUGUI rank_label;
        public TextMeshProUGUI role_name;
        public TextMeshProUGUI server_info;
        public TextMeshProUGUI fight;
        public TextMeshProUGUI pk_recore;
        public TextMeshProUGUI score;
        public RectTransform platform_conta;
        public TextMeshProUGUI win_state;
        public TextMeshProUGUI hp;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg_img), bg_img);
            EnsureBound(nameof(rank_icon), rank_icon);
            EnsureBound(nameof(rank_label), rank_label);
            EnsureBound(nameof(role_name), role_name);
            EnsureBound(nameof(server_info), server_info);
            EnsureBound(nameof(fight), fight);
            EnsureBound(nameof(pk_recore), pk_recore);
            EnsureBound(nameof(score), score);
            EnsureBound(nameof(platform_conta), platform_conta);
            EnsureBound(nameof(win_state), win_state);
            EnsureBound(nameof(hp), hp);
        }
    }
}
