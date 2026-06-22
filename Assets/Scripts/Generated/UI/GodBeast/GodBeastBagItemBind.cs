// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBeast/GodBeastBagItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBeast
{
    public partial class GodBeastBagItemBind : BaseView
    {
        public RectTransform con;
        public Image _img_tips;
        public Image _img_down;
        public Image _img_up;
        public RectTransform _group_select;
        public Image _Image1;
        public Image _Image2;

        protected override void BindNodes()
        {
            EnsureBound(nameof(con), con);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_img_down), _img_down);
            EnsureBound(nameof(_img_up), _img_up);
            EnsureBound(nameof(_group_select), _group_select);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
        }
    }
}
