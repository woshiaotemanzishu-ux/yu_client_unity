// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bindJageWish/BindJageWishRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BindJageWish
{
    public partial class BindJageWishRewardViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _Image4;
        public ScrollRect _rew_list;
        public Image _btn_close;
        public GameObject _tpl_BindJageRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_rew_list), _rew_list);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_tpl_BindJageRewardItem), _tpl_BindJageRewardItem);
        }
    }
}
