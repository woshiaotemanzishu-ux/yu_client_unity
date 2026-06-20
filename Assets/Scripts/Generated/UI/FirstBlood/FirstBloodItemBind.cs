// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/firstBlood/FirstBloodItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FirstBlood
{
    public partial class FirstBloodItemBind : BaseView
    {
        public Image image;
        public Image monster_img;
        public Image select_img;
        public TextMeshProUGUI name_text;
        public Image red_img;
        public Image killed_img;
        public TextMeshProUGUI _all_kill;

        protected override void BindNodes()
        {
            EnsureBound(nameof(image), image);
            EnsureBound(nameof(monster_img), monster_img);
            EnsureBound(nameof(select_img), select_img);
            EnsureBound(nameof(name_text), name_text);
            EnsureBound(nameof(red_img), red_img);
            EnsureBound(nameof(killed_img), killed_img);
            EnsureBound(nameof(_all_kill), _all_kill);
        }
    }
}
