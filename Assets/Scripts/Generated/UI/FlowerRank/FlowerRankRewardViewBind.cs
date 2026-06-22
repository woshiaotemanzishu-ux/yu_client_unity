// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/flowerRank/FlowerRankRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FlowerRank
{
    public partial class FlowerRankRewardViewBind : BaseView
    {
        public Image _img_close;
        public Image _img_bg;
        public ScrollRect _list_item;
        public TextMeshProUGUI _html_rank;
        public RectTransform _box_time;
        public GameObject _tpl_FlowerRankRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_html_rank), _html_rank);
            EnsureBound(nameof(_box_time), _box_time);
            EnsureBound(nameof(_tpl_FlowerRankRewardItem), _tpl_FlowerRankRewardItem);
        }
    }
}
