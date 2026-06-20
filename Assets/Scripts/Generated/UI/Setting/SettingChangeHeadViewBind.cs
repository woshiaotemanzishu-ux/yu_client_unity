// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/setting/SettingChangeHeadView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Setting
{
    public partial class SettingChangeHeadViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public RectTransform _Group1;
        public Image _Image3;
        public Image _Image4;
        public Image _Image5;
        public ScrollRect scroll;
        public RectTransform okBtn;
        public Image _Image6;
        public TextMeshProUGUI _Label1;
        public Image _close_btn;
        public GameObject _tpl_SettingHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(okBtn), okBtn);
            EnsureBound(nameof(_Image6), _Image6);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_close_btn), _close_btn);
            EnsureBound(nameof(_tpl_SettingHeadItem), _tpl_SettingHeadItem);
        }
    }
}
