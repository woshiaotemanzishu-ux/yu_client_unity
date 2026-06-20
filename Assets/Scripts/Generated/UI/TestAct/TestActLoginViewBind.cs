// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/testAct/TestActLoginView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.TestAct
{
    public partial class TestActLoginViewBind : BaseView
    {
        public Image tltle;
        public RectTransform effect;
        public Image _img_good;
        public TextMeshProUGUI _lb_desc2;
        public TextMeshProUGUI _lb_desc3;
        public TextMeshProUGUI _lb_desc4;
        public Image des_bg;
        public TextMeshProUGUI _lb_desc;

        protected override void BindNodes()
        {
            EnsureBound(nameof(tltle), tltle);
            EnsureBound(nameof(effect), effect);
            EnsureBound(nameof(_img_good), _img_good);
            EnsureBound(nameof(_lb_desc2), _lb_desc2);
            EnsureBound(nameof(_lb_desc3), _lb_desc3);
            EnsureBound(nameof(_lb_desc4), _lb_desc4);
            EnsureBound(nameof(des_bg), des_bg);
            EnsureBound(nameof(_lb_desc), _lb_desc);
        }
    }
}
