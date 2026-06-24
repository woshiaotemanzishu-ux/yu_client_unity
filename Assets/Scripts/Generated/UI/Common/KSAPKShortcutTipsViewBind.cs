// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/KSAPKShortcutTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class KSAPKShortcutTipsViewBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _list_reward;
        public Image _img_close;
        public Image _img_save;
        public TextMeshProUGUI _lb_ok;
        public GameObject _tpl_CommonRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_list_reward), _list_reward);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_save), _img_save);
            EnsureBound(nameof(_lb_ok), _lb_ok);
            EnsureBound(nameof(_tpl_CommonRewardItem), _tpl_CommonRewardItem);
        }
    }
}
