// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/country/CountryPoliticalItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Country
{
    public partial class CountryPoliticalItemBind : BaseView
    {
        public Image bg;
        public TextMeshProUGUI posLb;
        public TextMeshProUGUI exploitLb;
        public TextMeshProUGUI fightLb;
        public TextMeshProUGUI guildLb;
        public RectTransform _box_name;
        public Image vipImg;
        public TextMeshProUGUI nameLb;
        public RectTransform _box_lv;
        public Image lvImg;
        public TextMeshProUGUI lvLb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(posLb), posLb);
            EnsureBound(nameof(exploitLb), exploitLb);
            EnsureBound(nameof(fightLb), fightLb);
            EnsureBound(nameof(guildLb), guildLb);
            EnsureBound(nameof(_box_name), _box_name);
            EnsureBound(nameof(vipImg), vipImg);
            EnsureBound(nameof(nameLb), nameLb);
            EnsureBound(nameof(_box_lv), _box_lv);
            EnsureBound(nameof(lvImg), lvImg);
            EnsureBound(nameof(lvLb), lvLb);
        }
    }
}
