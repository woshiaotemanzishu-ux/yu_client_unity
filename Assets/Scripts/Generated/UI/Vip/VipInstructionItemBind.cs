// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/vip/VipInstructionItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Vip
{
    public partial class VipInstructionItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform info_hbox;
        public TextMeshProUGUI _lb_desc;
        public RectTransform new_group;
        public Image _Image2;
        public TextMeshProUGUI _Label1;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(info_hbox), info_hbox);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(new_group), new_group);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Label1), _Label1);
        }
    }
}
