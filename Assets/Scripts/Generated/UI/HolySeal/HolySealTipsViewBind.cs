// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holySeal/HolySealTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolySeal
{
    public partial class HolySealTipsViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _img_close;
        public TextMeshProUGUI _lb_title;
        public ScrollRect _list_item_con;
        public GameObject _tpl_InstructionItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_list_item_con), _list_item_con);
            EnsureBound(nameof(_tpl_InstructionItem), _tpl_InstructionItem);
        }
    }
}
