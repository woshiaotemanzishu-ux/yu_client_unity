// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildFight/GuildFightMapItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GuildFight
{
    public partial class GuildFightMapItemBind : BaseView
    {
        public RectTransform _box_click;
        public Image towerBg;
        public Image selectImg;
        public Image shadowImg;
        public Image leftBg;
        public Image _nameImg;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_qua;
        public Image vsImg;
        public TextMeshProUGUI guild1Lb;
        public TextMeshProUGUI guild2Lb;
        public Image lockImg;
        public RectTransform selectGp;
        public Image _Image2;
        public TextMeshProUGUI _Label1;
        public RectTransform effectGp;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_click), _box_click);
            EnsureBound(nameof(towerBg), towerBg);
            EnsureBound(nameof(selectImg), selectImg);
            EnsureBound(nameof(shadowImg), shadowImg);
            EnsureBound(nameof(leftBg), leftBg);
            EnsureBound(nameof(_nameImg), _nameImg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_qua), _lb_qua);
            EnsureBound(nameof(vsImg), vsImg);
            EnsureBound(nameof(guild1Lb), guild1Lb);
            EnsureBound(nameof(guild2Lb), guild2Lb);
            EnsureBound(nameof(lockImg), lockImg);
            EnsureBound(nameof(selectGp), selectGp);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(effectGp), effectGp);
        }
    }
}
