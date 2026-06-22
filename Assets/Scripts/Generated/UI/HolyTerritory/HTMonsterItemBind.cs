// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyTerritory/HTMonsterItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyTerritory
{
    public partial class HTMonsterItemBind : BaseView
    {
        public RectTransform clickGroup;
        public Image headBG;
        public Image shengzhu_bg;
        public Image monsterHead;
        public Image red;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI timeLabel;
        public RectTransform peaceGp;
        public Image _Image1;
        public TextMeshProUGUI _Label1;
        public Image shengzhu_alive;

        protected override void BindNodes()
        {
            EnsureBound(nameof(clickGroup), clickGroup);
            EnsureBound(nameof(headBG), headBG);
            EnsureBound(nameof(shengzhu_bg), shengzhu_bg);
            EnsureBound(nameof(monsterHead), monsterHead);
            EnsureBound(nameof(red), red);
            EnsureBound(nameof(nameLabel), nameLabel);
            EnsureBound(nameof(timeLabel), timeLabel);
            EnsureBound(nameof(peaceGp), peaceGp);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(shengzhu_alive), shengzhu_alive);
        }
    }
}
