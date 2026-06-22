// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonExp/DoubleExpTipView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonExp
{
    public partial class DoubleExpTipViewBind : BaseView
    {
        public Image btn_close;
        public Image img_bg;
        public Image icon;
        public Image img_title;
        public Image des;
        public Image img_nameBg;
        public TextMeshProUGUI lb_name;
        public Image img_line;
        public TextMeshProUGUI lb_hit;
        public RectTransform _gp_good_info;
        public RectTransform _gp_goods;
        public TextMeshProUGUI _lb_goods_name;
        public TextMeshProUGUI _lb_goods_num;
        public RectTransform _gp_btn;
        public Image _Image4;
        public TextMeshProUGUI _lb_btn;
        public Image _img_red;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(btn_close), btn_close);
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(des), des);
            EnsureBound(nameof(img_nameBg), img_nameBg);
            EnsureBound(nameof(lb_name), lb_name);
            EnsureBound(nameof(img_line), img_line);
            EnsureBound(nameof(lb_hit), lb_hit);
            EnsureBound(nameof(_gp_good_info), _gp_good_info);
            EnsureBound(nameof(_gp_goods), _gp_goods);
            EnsureBound(nameof(_lb_goods_name), _lb_goods_name);
            EnsureBound(nameof(_lb_goods_num), _lb_goods_num);
            EnsureBound(nameof(_gp_btn), _gp_btn);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_lb_btn), _lb_btn);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
