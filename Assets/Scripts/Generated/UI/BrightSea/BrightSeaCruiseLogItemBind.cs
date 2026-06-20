// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/brightSea/BrightSeaCruiseLogItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BrightSea
{
    public partial class BrightSeaCruiseLogItemBind : BaseView
    {
        public Image _img_line;
        public TextMeshProUGUI _lb_time;
        public TextMeshProUGUI _lb_content;
        public RectTransform _btn_guild_help;
        public Image _Image6;
        public TextMeshProUGUI _Label41;
        public RectTransform _btn_plunder;
        public Image _Image61;
        public TextMeshProUGUI _Label4;
        public Image _img_pld_success;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_line), _img_line);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_lb_content), _lb_content);
            EnsureBound(nameof(_btn_guild_help), _btn_guild_help);
            EnsureBound(nameof(_Image6), _Image6);
            EnsureBound(nameof(_Label41), _Label41);
            EnsureBound(nameof(_btn_plunder), _btn_plunder);
            EnsureBound(nameof(_Image61), _Image61);
            EnsureBound(nameof(_Label4), _Label4);
            EnsureBound(nameof(_img_pld_success), _img_pld_success);
        }
    }
}
