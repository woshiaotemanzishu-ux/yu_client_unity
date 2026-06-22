// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dress/DressSkillItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Dress
{
    public partial class DressSkillItemBind : BaseView
    {
        public Image _Image1;
        public Image skill_img;
        public Image condition_bg;
        public TextMeshProUGUI condition;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(skill_img), skill_img);
            EnsureBound(nameof(condition_bg), condition_bg);
            EnsureBound(nameof(condition), condition);
        }
    }
}
