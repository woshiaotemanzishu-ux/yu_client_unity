// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaSceneDamageItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaSceneDamageItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI _lb_role_name;
        public TextMeshProUGUI _lb_role_damage;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_role_name), _lb_role_name);
            EnsureBound(nameof(_lb_role_damage), _lb_role_damage);
        }
    }
}
