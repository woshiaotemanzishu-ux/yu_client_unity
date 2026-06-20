// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageRecordFlowerItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageRecordFlowerItemBind : BaseView
    {
        public Image _img_080;
        public Image _btn_thank;
        public Image _btn_gift;
        public TextMeshProUGUI _lb_content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_080), _img_080);
            EnsureBound(nameof(_btn_thank), _btn_thank);
            EnsureBound(nameof(_btn_gift), _btn_gift);
            EnsureBound(nameof(_lb_content), _lb_content);
        }
    }
}
