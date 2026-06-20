// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/deposit/DepositItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Deposit
{
    public partial class DepositItemBind : BaseView
    {
        public Image bg;
        public TextMeshProUGUI title;
        public Image icon;
        public TextMeshProUGUI time;
        public TextMeshProUGUI condition;
        public RectTransform click_bg;
        public RectTransform _cb_auto;
        public Image _Image1;
        public Image CheckMask;
        public TextMeshProUGUI labelDisplay;
        public TextMeshProUGUI txt1;
        public TextMeshProUGUI txt2;
        public TextMeshProUGUI txt_setting;
        public Image img1;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(time), time);
            EnsureBound(nameof(condition), condition);
            EnsureBound(nameof(click_bg), click_bg);
            EnsureBound(nameof(_cb_auto), _cb_auto);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(CheckMask), CheckMask);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(txt1), txt1);
            EnsureBound(nameof(txt2), txt2);
            EnsureBound(nameof(txt_setting), txt_setting);
            EnsureBound(nameof(img1), img1);
        }
    }
}
