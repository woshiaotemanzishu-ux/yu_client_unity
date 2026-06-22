// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaSceneBossItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaSceneBossItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI _lb_boss_name;
        public TextMeshProUGUI _lb_boss_tips;
        public Image _Image2;
        public Image _img_peace;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_boss_name), _lb_boss_name);
            EnsureBound(nameof(_lb_boss_tips), _lb_boss_tips);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_img_peace), _img_peace);
        }
    }
}
