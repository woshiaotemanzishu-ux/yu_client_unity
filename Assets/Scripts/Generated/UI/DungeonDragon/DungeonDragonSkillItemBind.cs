// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonDragon/DungeonDragonSkillItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonDragon
{
    public partial class DungeonDragonSkillItemBind : BaseView
    {
        public Image _img_skill;
        public TextMeshProUGUI _lb_left_count;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_skill), _img_skill);
            EnsureBound(nameof(_lb_left_count), _lb_left_count);
        }
    }
}
