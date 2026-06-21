// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildApplyLookItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildApplyLookItemBind : BaseView
    {
        public Image _Image111;
        public RectTransform _playerHead;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _lb_fight;
        public RectTransform _btn_pass;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public RectTransform _btn_refuse;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay1;
        public RectTransform _Group1;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_level;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image111), _Image111);
            EnsureBound(nameof(_playerHead), _playerHead);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_lb_fight), _lb_fight);
            EnsureBound(nameof(_btn_pass), _btn_pass);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_btn_refuse), _btn_refuse);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay1), labelDisplay1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_level), _lb_level);
        }
    }
}
