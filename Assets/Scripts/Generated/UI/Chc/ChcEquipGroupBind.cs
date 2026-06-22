// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chc/chcEquipGroup.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chc
{
    public partial class ChcEquipGroupBind : BaseView
    {
        public Image _img_bg1;
        public Image _img_bg2;
        public ScrollRect _Scroller1;
        public ScrollRect _Scroller2;
        public GameObject _tpl_chcEquipItem;
        public GameObject _tpl_chcTypeBtn;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg1), _img_bg1);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_Scroller2), _Scroller2);
            EnsureBound(nameof(_tpl_chcEquipItem), _tpl_chcEquipItem);
            EnsureBound(nameof(_tpl_chcTypeBtn), _tpl_chcTypeBtn);
        }
    }
}
