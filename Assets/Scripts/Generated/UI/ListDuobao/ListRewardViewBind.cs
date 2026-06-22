// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/listDuobao/ListRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.ListDuobao
{
    public partial class ListRewardViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _btn_close;
        public Image _img_title;
        public ScrollRect _gp_reward;
        public GameObject _tpl_ListRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_tpl_ListRewardItem), _tpl_ListRewardItem);
        }
    }
}
