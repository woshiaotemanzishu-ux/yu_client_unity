// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildMailView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildMailViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2_title;
        public TextMeshProUGUI _lb_win_name;
        public Image _Image2;
        public RectTransform _btn_send;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay1;
        public RectTransform _btn_close;
        public Image _Image111;
        public Image _Image6;
        public TMP_InputField _input;
        public TextMeshProUGUI Text;
        public TextMeshProUGUI _lb_limit;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2_title), _Image2_title);
            EnsureBound(nameof(_lb_win_name), _lb_win_name);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_btn_send), _btn_send);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay1), labelDisplay1);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Image111), _Image111);
            EnsureBound(nameof(_Image6), _Image6);
            EnsureBound(nameof(_input), _input);
            EnsureBound(nameof(Text), Text);
            EnsureBound(nameof(_lb_limit), _lb_limit);
        }
    }
}
