// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfSingleRank/kfSRRankView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfSingleRank
{
    public partial class KfSRRankViewBind : BaseView
    {
        public Image _Image1;
        public Image _img_title;
        public TextMeshProUGUI _lb_t;
        public Image _Image2;
        public Image _btn_close;
        public RectTransform _Group1;
        public Image _Image5;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _Label3;
        public TextMeshProUGUI _Label4;
        public ScrollRect Content;
        public GameObject _tpl_kfSRRankItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_t), _lb_t);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_Label3), _Label3);
            EnsureBound(nameof(_Label4), _Label4);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_kfSRRankItem), _tpl_kfSRRankItem);
        }
    }
}
