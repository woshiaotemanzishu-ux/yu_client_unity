// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/DownDropBtn.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class DownDropBtnBind : BaseView
    {
        public RectTransform _group;
        public Image _Image1;
        public Image _img_arrow;
        public TextMeshProUGUI _lb_content;
        public GameObject _tpl_DownDropItem;
        public GameObject _tpl_DownDropList;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_group), _group);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_arrow), _img_arrow);
            EnsureBound(nameof(_lb_content), _lb_content);
            EnsureBound(nameof(_tpl_DownDropItem), _tpl_DownDropItem);
            EnsureBound(nameof(_tpl_DownDropList), _tpl_DownDropList);
        }
    }
}
