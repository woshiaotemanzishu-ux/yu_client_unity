// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildBoardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildBoardViewBind : BaseView
    {
        public Image _Image111;
        public Image _Image4;
        public Image _Image5;
        public TextMeshProUGUI Placeholder;
        public TextMeshProUGUI Text1;
        public RectTransform _btn_go;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public RectTransform _btn_close;
        public Image _Image11;
        public TMP_InputField _edit_text;
        public TextMeshProUGUI _lb_limit;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image111), _Image111);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(Placeholder), Placeholder);
            EnsureBound(nameof(Text1), Text1);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_edit_text), _edit_text);
            EnsureBound(nameof(_lb_limit), _lb_limit);
        }
    }
}
