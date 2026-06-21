// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildMemberView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildMemberViewBind : BaseView
    {
        public Image _Image3;
        public ScrollRect _list_menber;
        public RectTransform _Group2;
        public RectTransform _btn_set;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay1;
        public RectTransform _btn_apply;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public Image red;
        public GameObject _tpl_GuildMemberItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_list_menber), _list_menber);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_btn_set), _btn_set);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay1), labelDisplay1);
            EnsureBound(nameof(_btn_apply), _btn_apply);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(red), red);
            EnsureBound(nameof(_tpl_GuildMemberItem), _tpl_GuildMemberItem);
        }
    }
}
