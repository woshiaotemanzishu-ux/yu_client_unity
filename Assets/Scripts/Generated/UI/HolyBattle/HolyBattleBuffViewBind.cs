// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyBattle/HolyBattleBuffView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyBattle
{
    public partial class HolyBattleBuffViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _lb_win_name;
        public Image _img_close;
        public Image _Image4;
        public TextMeshProUGUI _lb_tips;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _lb_attr;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_win_name), _lb_win_name);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_lb_attr), _lb_attr);
        }
    }
}
