// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/RegisterView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class RegisterViewBind : BaseView
    {
        public Image bg;
        public RectTransform _Group1;
        public TextMeshProUGUI _Label1;
        public RectTransform _Group2;
        public TextMeshProUGUI _Label2;
        public Image logo;
        public RectTransform confirmBtn;
        public Image _Image1;
        public TextMeshProUGUI _Label3;
        public RectTransform returnBtn;
        public Image _Image2;
        public TextMeshProUGUI _Label4;
        public TMP_InputField account;
        public TMP_InputField password;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(logo), logo);
            EnsureBound(nameof(confirmBtn), confirmBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Label3), _Label3);
            EnsureBound(nameof(returnBtn), returnBtn);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Label4), _Label4);
            EnsureBound(nameof(account), account);
            EnsureBound(nameof(password), password);
        }
    }
}
