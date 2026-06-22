// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/boss/BossTargetSubItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Boss
{
    public partial class BossTargetSubItemBind : BaseView
    {
        public RectTransform _box_con;
        public Image _img_bg;
        public Image _Image2;
        public Image _img_head;
        public RectTransform _box_head;
        public TextMeshProUGUI _lb_count;
        public RectTransform _box_progress;
        public Image _img_progress_bar;
        public TextMeshProUGUI _lb_lv;
        public TextMeshProUGUI _lb_name;
        public Image _img_owner;
        public RectTransform _box_attack_effect;
        public RectTransform _box_suffer_effect;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_img_head), _img_head);
            EnsureBound(nameof(_box_head), _box_head);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_box_progress), _box_progress);
            EnsureBound(nameof(_img_progress_bar), _img_progress_bar);
            EnsureBound(nameof(_lb_lv), _lb_lv);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_img_owner), _img_owner);
            EnsureBound(nameof(_box_attack_effect), _box_attack_effect);
            EnsureBound(nameof(_box_suffer_effect), _box_suffer_effect);
        }
    }
}
