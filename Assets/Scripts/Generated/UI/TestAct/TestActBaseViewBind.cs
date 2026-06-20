// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/testAct/TestActBaseView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.TestAct
{
    public partial class TestActBaseViewBind : BaseView
    {
        public Image _img_bg;
        public RectTransform loginGp;
        public RectTransform rechargeGp;
        public RectTransform btn_1;
        public Image img_btn_1;
        public RectTransform btn_2;
        public Image img_btn_2;
        public Image _btn_close;
        public GameObject _tpl_TestActLoginView;
        public GameObject _tpl_TestActRecharge;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(loginGp), loginGp);
            EnsureBound(nameof(rechargeGp), rechargeGp);
            EnsureBound(nameof(btn_1), btn_1);
            EnsureBound(nameof(img_btn_1), img_btn_1);
            EnsureBound(nameof(btn_2), btn_2);
            EnsureBound(nameof(img_btn_2), img_btn_2);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_tpl_TestActLoginView), _tpl_TestActLoginView);
            EnsureBound(nameof(_tpl_TestActRecharge), _tpl_TestActRecharge);
        }
    }
}
