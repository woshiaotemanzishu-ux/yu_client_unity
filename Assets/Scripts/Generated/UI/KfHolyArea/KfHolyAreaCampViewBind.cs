// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaCampView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaCampViewBind : BaseView
    {
        public Image _Image1;
        public Image _img_close;
        public Image _Image2;
        public Image _Image3;
        public Image _Image4;
        public ScrollRect Content;
        public GameObject _tpl_KfHolyAreaCampItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_KfHolyAreaCampItem), _tpl_KfHolyAreaCampItem);
        }
    }
}
