// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaServerView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaServerViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_close;
        public Image _img_tips;
        public Image _img_bg2;
        public TextMeshProUGUI _html_desc;
        public TextMeshProUGUI _html_desc2;
        public ScrollRect _list_item;
        public GameObject _tpl_KfHolyAreaServerItem1;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_html_desc), _html_desc);
            EnsureBound(nameof(_html_desc2), _html_desc2);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_tpl_KfHolyAreaServerItem1), _tpl_KfHolyAreaServerItem1);
        }
    }
}
