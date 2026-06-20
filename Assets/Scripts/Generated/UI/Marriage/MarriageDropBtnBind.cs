// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageDropBtn.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageDropBtnBind : BaseView
    {
        public RectTransform _group;
        public Image _Image1;
        public Image _img_arrow;
        public TextMeshProUGUI _lb_content;
        public GameObject _tpl_MarriageDropList;
        public GameObject _tpl_MarriageDropItem;
        public GameObject _tpl_DownDropBtn;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_group), _group);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_arrow), _img_arrow);
            EnsureBound(nameof(_lb_content), _lb_content);
            EnsureBound(nameof(_tpl_MarriageDropList), _tpl_MarriageDropList);
            EnsureBound(nameof(_tpl_MarriageDropItem), _tpl_MarriageDropItem);
            EnsureBound(nameof(_tpl_DownDropBtn), _tpl_DownDropBtn);
        }
    }
}
