// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/setting/SettingHeadItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Setting
{
    public partial class SettingHeadItemBind : BaseView
    {
        public Image _Image1;
        public Image _img_take_photo;
        public RectTransform role_head;
        public Image _img_frame;
        public Image _img_role_head;
        public RectTransform _Group1;
        public Image _Image2;
        public TextMeshProUGUI _lb_name;
        public Image _img_select;
        public Image use_tag;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_take_photo), _img_take_photo);
            EnsureBound(nameof(role_head), role_head);
            EnsureBound(nameof(_img_frame), _img_frame);
            EnsureBound(nameof(_img_role_head), _img_role_head);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(use_tag), use_tag);
        }
    }
}
