// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossField/BossFieldTiredNewView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossField
{
    public partial class BossFieldTiredNewViewBind : BaseView
    {
        public Image close;
        public Image bg1;
        public RectTransform _box_info;
        public TextMeshProUGUI tired;
        public TextMeshProUGUI tired_tip;
        public TextMeshProUGUI _lb_time;
        public RectTransform item_gp;
        public RectTransform use;
        public Image btn_img;
        public TextMeshProUGUI btn_text;
        public RectTransform left_gp;
        public Image left_vip;
        public TextMeshProUGUI left_tired;
        public RectTransform right_gp;
        public Image right_vip;
        public TextMeshProUGUI right_tired;
        public Image arrow;
        public TextMeshProUGUI tip;
        public TextMeshProUGUI go;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(close), close);
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(_box_info), _box_info);
            EnsureBound(nameof(tired), tired);
            EnsureBound(nameof(tired_tip), tired_tip);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(item_gp), item_gp);
            EnsureBound(nameof(use), use);
            EnsureBound(nameof(btn_img), btn_img);
            EnsureBound(nameof(btn_text), btn_text);
            EnsureBound(nameof(left_gp), left_gp);
            EnsureBound(nameof(left_vip), left_vip);
            EnsureBound(nameof(left_tired), left_tired);
            EnsureBound(nameof(right_gp), right_gp);
            EnsureBound(nameof(right_vip), right_vip);
            EnsureBound(nameof(right_tired), right_tired);
            EnsureBound(nameof(arrow), arrow);
            EnsureBound(nameof(tip), tip);
            EnsureBound(nameof(go), go);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
