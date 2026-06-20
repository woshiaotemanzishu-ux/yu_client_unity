// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/CustomerServiceView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class CustomerServiceViewBind : BaseView
    {
        public Image Bg;
        public Image _Image1;
        public RectTransform submitBtn;
        public TextMeshProUGUI btnLabel;
        public TextMeshProUGUI Placeholder;
        public TextMeshProUGUI Text;
        public RectTransform downBox;
        public RectTransform upBox;
        public Image bg1;
        public Image bg2;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public Image _Image2;
        public TMP_InputField inputLabel;
        public GameObject _tpl_CustomerServiceItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(Bg), Bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(submitBtn), submitBtn);
            EnsureBound(nameof(btnLabel), btnLabel);
            EnsureBound(nameof(Placeholder), Placeholder);
            EnsureBound(nameof(Text), Text);
            EnsureBound(nameof(downBox), downBox);
            EnsureBound(nameof(upBox), upBox);
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(bg2), bg2);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(inputLabel), inputLabel);
            EnsureBound(nameof(_tpl_CustomerServiceItem), _tpl_CustomerServiceItem);
        }
    }
}
