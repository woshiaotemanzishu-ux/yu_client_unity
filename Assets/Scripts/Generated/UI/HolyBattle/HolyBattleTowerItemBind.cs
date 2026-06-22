// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyBattle/HolyBattleTowerItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyBattle
{
    public partial class HolyBattleTowerItemBind : BaseView
    {
        public Image _img_bg;
        public Image _Image1;
        public Image _img_tips;
        public TextMeshProUGUI _lb_name;
        public Image _Image2;
        public Image _img_hp;
        public RectTransform _gp_go;
        public Image _img_go;
        public TextMeshProUGUI _lb_go;
        public TextMeshProUGUI _lb_hp;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_img_hp), _img_hp);
            EnsureBound(nameof(_gp_go), _gp_go);
            EnsureBound(nameof(_img_go), _img_go);
            EnsureBound(nameof(_lb_go), _lb_go);
            EnsureBound(nameof(_lb_hp), _lb_hp);
        }
    }
}
