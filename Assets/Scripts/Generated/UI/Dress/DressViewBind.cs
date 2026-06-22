// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dress/DressView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Dress
{
    public partial class DressViewBind : BaseView
    {
        public RectTransform sub_con;
        public ScrollRect _Scroller1;
        public GameObject _tpl_DressSubView;
        public GameObject _tpl_DressItem;
        public GameObject _tpl_DressProItem;
        public GameObject _tpl_DressSkillItem;
        public GameObject _tpl_DressTab;

        protected override void BindNodes()
        {
            EnsureBound(nameof(sub_con), sub_con);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_tpl_DressSubView), _tpl_DressSubView);
            EnsureBound(nameof(_tpl_DressItem), _tpl_DressItem);
            EnsureBound(nameof(_tpl_DressProItem), _tpl_DressProItem);
            EnsureBound(nameof(_tpl_DressSkillItem), _tpl_DressSkillItem);
            EnsureBound(nameof(_tpl_DressTab), _tpl_DressTab);
        }
    }
}
