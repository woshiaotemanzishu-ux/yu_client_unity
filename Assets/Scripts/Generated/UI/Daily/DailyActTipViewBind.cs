// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/daily/DailyActTipView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Daily
{
    public partial class DailyActTipViewBind : BaseView
    {
        public Image bg;
        public Image bg1;
        public Image title;
        public Image bg2;
        public Image close;
        public Image ins;
        public RectTransform yuyue_box;
        public Image yuyue;
        public Image yuyue_red;
        public Image go;
        public TextMeshProUGUI act_num;
        public RectTransform tip;
        public Image check;
        public Image toggle;
        public TextMeshProUGUI time;
        public ScrollRect reward_panel;
        public RectTransform reward_gp;
        public ScrollRect tab_gp;
        public GameObject _tpl_DailyActTipTab;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(bg2), bg2);
            EnsureBound(nameof(close), close);
            EnsureBound(nameof(ins), ins);
            EnsureBound(nameof(yuyue_box), yuyue_box);
            EnsureBound(nameof(yuyue), yuyue);
            EnsureBound(nameof(yuyue_red), yuyue_red);
            EnsureBound(nameof(go), go);
            EnsureBound(nameof(act_num), act_num);
            EnsureBound(nameof(tip), tip);
            EnsureBound(nameof(check), check);
            EnsureBound(nameof(toggle), toggle);
            EnsureBound(nameof(time), time);
            EnsureBound(nameof(reward_panel), reward_panel);
            EnsureBound(nameof(reward_gp), reward_gp);
            EnsureBound(nameof(tab_gp), tab_gp);
            EnsureBound(nameof(_tpl_DailyActTipTab), _tpl_DailyActTipTab);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
