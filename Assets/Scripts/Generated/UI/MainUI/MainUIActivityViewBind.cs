// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIActivityView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIActivityViewBind : BaseView
    {
        public RectTransform _gp_con;
        public Image _img_turn;
        public RectTransform _box_rank;
        public Image _img_bg;
        public RectTransform effect;
        public RectTransform icon_box;
        public RectTransform model_gp;
        public Image icon;
        public Image bg1;
        public TextMeshProUGUI name;
        public TextMeshProUGUI player_name;
        public TextMeshProUGUI time;
        public Image _img_red;
        public GameObject _tpl_EquipmentItem;
        public GameObject _tpl_ActivityIcon;
        public GameObject _tpl_TopPlayerTipItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_img_turn), _img_turn);
            EnsureBound(nameof(_box_rank), _box_rank);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(effect), effect);
            EnsureBound(nameof(icon_box), icon_box);
            EnsureBound(nameof(model_gp), model_gp);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(name), name);
            EnsureBound(nameof(player_name), player_name);
            EnsureBound(nameof(time), time);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
            EnsureBound(nameof(_tpl_ActivityIcon), _tpl_ActivityIcon);
            EnsureBound(nameof(_tpl_TopPlayerTipItem), _tpl_TopPlayerTipItem);
        }
    }
}
