// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/growthForce/GrowthForceView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GrowthForce
{
    public partial class GrowthForceViewBind : BaseView
    {
        public Image bgImg;
        public RectTransform closeBox;
        public Image nameImg;
        public Image titleImg1;
        public ScrollRect tabList;
        public RectTransform viewBox1;
        public RectTransform viewBox2;
        public GameObject _tpl_GrowthFightWelfareView;
        public GameObject _tpl_GrowthFightWelfareItem;
        public GameObject _tpl_GrowthForceTabItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bgImg), bgImg);
            EnsureBound(nameof(closeBox), closeBox);
            EnsureBound(nameof(nameImg), nameImg);
            EnsureBound(nameof(titleImg1), titleImg1);
            EnsureBound(nameof(tabList), tabList);
            EnsureBound(nameof(viewBox1), viewBox1);
            EnsureBound(nameof(viewBox2), viewBox2);
            EnsureBound(nameof(_tpl_GrowthFightWelfareView), _tpl_GrowthFightWelfareView);
            EnsureBound(nameof(_tpl_GrowthFightWelfareItem), _tpl_GrowthFightWelfareItem);
            EnsureBound(nameof(_tpl_GrowthForceTabItem), _tpl_GrowthForceTabItem);
        }
    }
}
