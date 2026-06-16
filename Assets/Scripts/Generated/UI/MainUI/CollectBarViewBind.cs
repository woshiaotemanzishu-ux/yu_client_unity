// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/CollectBarView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class CollectBarViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_progress;
        public Image _img_num_bg;
        public TextMeshProUGUI _txt_progress;
        public Image _img_state;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_progress), _img_progress);
            EnsureBound(nameof(_img_num_bg), _img_num_bg);
            EnsureBound(nameof(_txt_progress), _txt_progress);
            EnsureBound(nameof(_img_state), _img_state);
        }
    }
}
