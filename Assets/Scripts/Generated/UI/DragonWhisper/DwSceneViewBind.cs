// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonWhisper/dwSceneView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonWhisper
{
    public partial class DwSceneViewBind : BaseView
    {
        public RectTransform _gp_time;
        public Image _img_bg;
        public Image _img_fight;
        public TextMeshProUGUI _lb_time;
        public RectTransform _gp_con;
        public Image _btn_bag;
        public RectTransform _gp_target;
        public Image _btn_exit;
        public GameObject _tpl_dwBossPanel;
        public GameObject _tpl_dwMonItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_fight), _img_fight);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_btn_bag), _btn_bag);
            EnsureBound(nameof(_gp_target), _gp_target);
            EnsureBound(nameof(_btn_exit), _btn_exit);
            EnsureBound(nameof(_tpl_dwBossPanel), _tpl_dwBossPanel);
            EnsureBound(nameof(_tpl_dwMonItem), _tpl_dwMonItem);
        }
    }
}
