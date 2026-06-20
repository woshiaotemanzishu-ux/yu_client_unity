// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/levelReward/LevelRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LevelReward
{
    public partial class LevelRewardViewBind : BaseView
    {
        public Image _img_title;
        public Image _img_tips;
        public Image _Image1;
        public ScrollRect _Scroller1;
        public ScrollRect _list_item_con;
        public Image _Image2;
        public TextMeshProUGUI _Label1;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_list_item_con), _list_item_con);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Label1), _Label1);
        }
    }
}
