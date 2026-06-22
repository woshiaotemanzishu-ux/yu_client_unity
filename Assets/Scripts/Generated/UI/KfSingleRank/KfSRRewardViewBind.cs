// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfSingleRank/kfSRRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfSingleRank
{
    public partial class KfSRRewardViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _img_title;
        public TextMeshProUGUI _lb_win_name;
        public Image _btn_close;
        public RectTransform _Group1;
        public Image _Image5;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _Label3;
        public ScrollRect Content;
        public GameObject _tpl_kfSRRewardItem;
        public GameObject _tpl_kfSRAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_win_name), _lb_win_name);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_Label3), _Label3);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_kfSRRewardItem), _tpl_kfSRRewardItem);
            EnsureBound(nameof(_tpl_kfSRAwardItem), _tpl_kfSRAwardItem);
        }
    }
}
