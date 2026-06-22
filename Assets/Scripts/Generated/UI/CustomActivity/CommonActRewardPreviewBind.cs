// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/customActivity/CommonActRewardPreview.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.CustomActivity
{
    public partial class CommonActRewardPreviewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public RectTransform _Group1;
        public Image _Image3;
        public Image _Image4;
        public ScrollRect _scr_reward;
        public RectTransform Content;
        public Image _img_close;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_scr_reward), _scr_reward);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
