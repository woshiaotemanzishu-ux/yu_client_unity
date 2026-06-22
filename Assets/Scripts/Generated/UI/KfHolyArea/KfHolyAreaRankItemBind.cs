// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaRankItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaRankItemBind : BaseView
    {
        public Image bg_img;
        public RectTransform _Group1;
        public Image rank_img;
        public TextMeshProUGUI rank_lb;
        public TextMeshProUGUI name_lb;
        public TextMeshProUGUI kill_cnt_lb;
        public TextMeshProUGUI contribution_lb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg_img), bg_img);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(rank_img), rank_img);
            EnsureBound(nameof(rank_lb), rank_lb);
            EnsureBound(nameof(name_lb), name_lb);
            EnsureBound(nameof(kill_cnt_lb), kill_cnt_lb);
            EnsureBound(nameof(contribution_lb), contribution_lb);
        }
    }
}
