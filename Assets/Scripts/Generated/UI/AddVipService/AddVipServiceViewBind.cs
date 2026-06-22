// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/addVipService/AddVipServiceView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.AddVipService
{
    public partial class AddVipServiceViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title;
        public Image _img_bg1;
        public Image _img_link;
        public Image bg3;
        public TextMeshProUGUI _lab_title;
        public RectTransform Content;
        public Image _img_bg2;
        public TextMeshProUGUI des;
        public Image close;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_img_bg1), _img_bg1);
            EnsureBound(nameof(_img_link), _img_link);
            EnsureBound(nameof(bg3), bg3);
            EnsureBound(nameof(_lab_title), _lab_title);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(des), des);
            EnsureBound(nameof(close), close);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
