// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnChallengerDetailView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnChallengerDetailViewBind : BaseView
    {
        public Image bg;
        public Image _title;
        public ScrollRect Content;
        public ScrollRect Content1;
        public GameObject _tpl_Kf1vnChallengerMsgItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_title), _title);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(_tpl_Kf1vnChallengerMsgItem), _tpl_Kf1vnChallengerMsgItem);
        }
    }
}
