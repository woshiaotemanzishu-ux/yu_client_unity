// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/ActivityIcon.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class ActivityIconBind : BaseView
    {
        public RectTransform _box_effect;
        public Image _img_icon;
        public Image _box_arrow;
        public Image _img_red;
        public Image _img_desc_bg;
        public Image _img_red_num;
        public TextMeshProUGUI _lb_num;
        public TextMeshProUGUI _lb_desc;
        public RectTransform _box_effect2;
        public GameObject _tpl_MainUIStrongerTalkBoard;
        public GameObject _tpl_TalkBoard;
        public GameObject _tpl_ArrowComponent;
        public GameObject _tpl_TopPlayerTipItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_effect), _box_effect);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_box_arrow), _box_arrow);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_img_desc_bg), _img_desc_bg);
            EnsureBound(nameof(_img_red_num), _img_red_num);
            EnsureBound(nameof(_lb_num), _lb_num);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_box_effect2), _box_effect2);
            EnsureBound(nameof(_tpl_MainUIStrongerTalkBoard), _tpl_MainUIStrongerTalkBoard);
            EnsureBound(nameof(_tpl_TalkBoard), _tpl_TalkBoard);
            EnsureBound(nameof(_tpl_ArrowComponent), _tpl_ArrowComponent);
            EnsureBound(nameof(_tpl_TopPlayerTipItem), _tpl_TopPlayerTipItem);
        }
    }
}
