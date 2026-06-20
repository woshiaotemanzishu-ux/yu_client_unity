// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonHeart/DungeonHeartEnterView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonHeart
{
    public partial class DungeonHeartEnterViewBind : BaseView
    {
        public Image bg;
        public RectTransform bossCon;
        public Image closeBtn;
        public TextMeshProUGUI bossName;
        public RectTransform enterBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public RectTransform _box_eff;
        public Image _img_tbg;
        public Image skillBg;
        public Image skillIcon;
        public TextMeshProUGUI skillDes;
        public TextMeshProUGUI skillName;
        public TextMeshProUGUI monsterText;
        public TextMeshProUGUI monsterText2;
        public TextMeshProUGUI monsterText3;
        public Image Image;
        public Image nameNum;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(bossCon), bossCon);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(bossName), bossName);
            EnsureBound(nameof(enterBtn), enterBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_box_eff), _box_eff);
            EnsureBound(nameof(_img_tbg), _img_tbg);
            EnsureBound(nameof(skillBg), skillBg);
            EnsureBound(nameof(skillIcon), skillIcon);
            EnsureBound(nameof(skillDes), skillDes);
            EnsureBound(nameof(skillName), skillName);
            EnsureBound(nameof(monsterText), monsterText);
            EnsureBound(nameof(monsterText2), monsterText2);
            EnsureBound(nameof(monsterText3), monsterText3);
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(nameNum), nameNum);
        }
    }
}
