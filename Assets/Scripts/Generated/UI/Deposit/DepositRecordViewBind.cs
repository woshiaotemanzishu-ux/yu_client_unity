// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/deposit/DepositRecordView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Deposit
{
    public partial class DepositRecordViewBind : BaseView
    {
        public Image bg;
        public Image img4;
        public Image img2;
        public ScrollRect sv;
        public RectTransform Content;
        public Image close;
        public TextMeshProUGUI Text;
        public RectTransform none_conta;
        public TextMeshProUGUI tips;
        public Image tips_icon;
        public GameObject _tpl_DepositRecordItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(img4), img4);
            EnsureBound(nameof(img2), img2);
            EnsureBound(nameof(sv), sv);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(close), close);
            EnsureBound(nameof(Text), Text);
            EnsureBound(nameof(none_conta), none_conta);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(tips_icon), tips_icon);
            EnsureBound(nameof(_tpl_DepositRecordItem), _tpl_DepositRecordItem);
        }
    }
}
