// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/newgameui/UIVerTabSubBtn2.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Newgameui
{
    public partial class UIVerTabSubBtn2Bind : BaseView
    {
        public RectTransform sub_conta;
        public Image btn_state;
        public Image down;
        public Image img;
        public Image red_dot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(sub_conta), sub_conta);
            EnsureBound(nameof(btn_state), btn_state);
            EnsureBound(nameof(down), down);
            EnsureBound(nameof(img), img);
            EnsureBound(nameof(red_dot), red_dot);
        }
    }
}
