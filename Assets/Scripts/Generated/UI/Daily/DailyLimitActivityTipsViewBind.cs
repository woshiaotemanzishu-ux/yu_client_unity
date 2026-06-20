// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/daily/DailyLimitActivityTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Daily
{
    public partial class DailyLimitActivityTipsViewBind : BaseView
    {
        public Image _Image1;
        public Image closeBtn;
        public Image _Image2;
        public Image icon;
        public Image _Image3;
        public TextMeshProUGUI name_str;
        public TextMeshProUGUI times;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _Label3;
        public TextMeshProUGUI level_limit;
        public TextMeshProUGUI time_length;
        public Image line1;
        public Image reward_bg;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public TextMeshProUGUI content;
        public TextMeshProUGUI _Label4;
        public ScrollRect _Scroller2;
        public RectTransform Content1;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(name_str), name_str);
            EnsureBound(nameof(times), times);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_Label3), _Label3);
            EnsureBound(nameof(level_limit), level_limit);
            EnsureBound(nameof(time_length), time_length);
            EnsureBound(nameof(line1), line1);
            EnsureBound(nameof(reward_bg), reward_bg);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(content), content);
            EnsureBound(nameof(_Label4), _Label4);
            EnsureBound(nameof(_Scroller2), _Scroller2);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
