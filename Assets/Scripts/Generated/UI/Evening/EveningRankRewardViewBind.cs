// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningRankRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningRankRewardViewBind : BaseView
    {
        public Image image1;
        public Image _image_title;
        public Image _img_close;
        public Image _Image2;
        public TextMeshProUGUI _lb_tips11;
        public ScrollRect _list_items;
        public TextMeshProUGUI _lb_1;
        public TextMeshProUGUI _lb_2;
        public Image _Image222;
        public TextMeshProUGUI _lb_win_name;
        public GameObject _tpl_EveningRankRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(image1), image1);
            EnsureBound(nameof(_image_title), _image_title);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_tips11), _lb_tips11);
            EnsureBound(nameof(_list_items), _list_items);
            EnsureBound(nameof(_lb_1), _lb_1);
            EnsureBound(nameof(_lb_2), _lb_2);
            EnsureBound(nameof(_Image222), _Image222);
            EnsureBound(nameof(_lb_win_name), _lb_win_name);
            EnsureBound(nameof(_tpl_EveningRankRewardItem), _tpl_EveningRankRewardItem);
        }
    }
}
