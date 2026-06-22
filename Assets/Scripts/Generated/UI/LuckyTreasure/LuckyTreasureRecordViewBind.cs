// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/luckyTreasure/LuckyTreasureRecordView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LuckyTreasure
{
    public partial class LuckyTreasureRecordViewBind : BaseView
    {
        public Image _Image111;
        public Image _Image4;
        public Image _img_close;
        public Image _Image5;
        public TextMeshProUGUI titleLabel;
        public RectTransform _box_record;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image111), _Image111);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(titleLabel), titleLabel);
            EnsureBound(nameof(_box_record), _box_record);
        }
    }
}
