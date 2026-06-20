// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageBaseBtn.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageBaseBtnBind : BaseView
    {
        public RectTransform con;
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_name;
        public TextMeshProUGUI tab_name;
        public Image _reddot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(con), con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_name), _img_name);
            EnsureBound(nameof(tab_name), tab_name);
            EnsureBound(nameof(_reddot), _reddot);
        }
    }
}
