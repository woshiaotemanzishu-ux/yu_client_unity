// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/testAct/TestActRecharge.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.TestAct
{
    public partial class TestActRechargeBind : BaseView
    {
        public Image tltle;
        public RectTransform effect;
        public Image _img_good;
        public RectTransform recharge_des;
        public Image _btn_ques;
        public TextMeshProUGUI _lb_recharge;
        public RectTransform go_btn;
        public TextMeshProUGUI _lb_desc;
        public TextMeshProUGUI _lb_desc2;
        public TextMeshProUGUI _lb_desc3;
        public Image des_bg;
        public TextMeshProUGUI _lb_return;

        protected override void BindNodes()
        {
            EnsureBound(nameof(tltle), tltle);
            EnsureBound(nameof(effect), effect);
            EnsureBound(nameof(_img_good), _img_good);
            EnsureBound(nameof(recharge_des), recharge_des);
            EnsureBound(nameof(_btn_ques), _btn_ques);
            EnsureBound(nameof(_lb_recharge), _lb_recharge);
            EnsureBound(nameof(go_btn), go_btn);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_lb_desc2), _lb_desc2);
            EnsureBound(nameof(_lb_desc3), _lb_desc3);
            EnsureBound(nameof(des_bg), des_bg);
            EnsureBound(nameof(_lb_return), _lb_return);
        }
    }
}
