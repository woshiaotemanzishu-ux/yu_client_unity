// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/svip/SvipMainView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Svip
{
    public partial class SvipMainViewBind : BaseView
    {
        public Image image_bg;
        public Image image_title;
        public Image btn_close;
        public Image image_role_icon;
        public Image image_role_frame;
        public Image image_desc;
        public RectTransform box_middle;
        public Image image_middle_tittle;
        public Image image_add_customer;
        public Image btn_bg;
        public Image image_contact_us;
        public TextMeshProUGUI lable_content;
        public GameObject _tpl_SvipMainItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(image_bg), image_bg);
            EnsureBound(nameof(image_title), image_title);
            EnsureBound(nameof(btn_close), btn_close);
            EnsureBound(nameof(image_role_icon), image_role_icon);
            EnsureBound(nameof(image_role_frame), image_role_frame);
            EnsureBound(nameof(image_desc), image_desc);
            EnsureBound(nameof(box_middle), box_middle);
            EnsureBound(nameof(image_middle_tittle), image_middle_tittle);
            EnsureBound(nameof(image_add_customer), image_add_customer);
            EnsureBound(nameof(btn_bg), btn_bg);
            EnsureBound(nameof(image_contact_us), image_contact_us);
            EnsureBound(nameof(lable_content), lable_content);
            EnsureBound(nameof(_tpl_SvipMainItem), _tpl_SvipMainItem);
        }
    }
}
