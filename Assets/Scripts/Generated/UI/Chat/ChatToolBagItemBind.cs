// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chat/ChatToolBagItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chat
{
    public partial class ChatToolBagItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform itemGroup;
        public Image _img_tips;
        public Image _img_down;
        public Image _img_up;
        public Image _img_ban;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(itemGroup), itemGroup);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_img_down), _img_down);
            EnsureBound(nameof(_img_up), _img_up);
            EnsureBound(nameof(_img_ban), _img_ban);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
