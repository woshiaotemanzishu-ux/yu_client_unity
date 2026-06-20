// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equip/EquipWashGoodsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Equip
{
    public partial class EquipWashGoodsViewBind : BaseView
    {
        public Image _Image11;
        public Image _img_close;
        public Image _Image4;
        public Image _Image3;
        public TextMeshProUGUI _Label1;
        public ScrollRect _scr_info;
        public RectTransform Content;
        public Image btn_unload;
        public TextMeshProUGUI lb_unload;
        public GameObject _tpl_EquipWashGoodsItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_scr_info), _scr_info);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(btn_unload), btn_unload);
            EnsureBound(nameof(lb_unload), lb_unload);
            EnsureBound(nameof(_tpl_EquipWashGoodsItem), _tpl_EquipWashGoodsItem);
        }
    }
}
