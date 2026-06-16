// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIIconBoxView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIIconBoxViewBind : BaseView
    {
        public Image bg;
        public RectTransform icon_con;
        public Image arrow;
        public GameObject _tpl_ActivityIcon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(icon_con), icon_con);
            EnsureBound(nameof(arrow), arrow);
            EnsureBound(nameof(_tpl_ActivityIcon), _tpl_ActivityIcon);
        }
    }
}
