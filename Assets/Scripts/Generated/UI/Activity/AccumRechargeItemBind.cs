// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/activity/AccumRechargeItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Activity
{
    public partial class AccumRechargeItemBind : BaseView
    {
        public Image _img_bg;
        public Image _Image111;
        public Image _img_over;
        public RectTransform _btn_recharge;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public RectTransform _btn_get;
        public Image _Image1;
        public Image _reddot;
        public TextMeshProUGUI labelDisplay1;
        public ScrollRect _gp_reward;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _lb_progress;
        public TextMeshProUGUI _lb_task_pg;
        public GameObject _tpl_CommonRewardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Image111), _Image111);
            EnsureBound(nameof(_img_over), _img_over);
            EnsureBound(nameof(_btn_recharge), _btn_recharge);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_btn_get), _btn_get);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_reddot), _reddot);
            EnsureBound(nameof(labelDisplay1), labelDisplay1);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_lb_progress), _lb_progress);
            EnsureBound(nameof(_lb_task_pg), _lb_task_pg);
            EnsureBound(nameof(_tpl_CommonRewardItem), _tpl_CommonRewardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
