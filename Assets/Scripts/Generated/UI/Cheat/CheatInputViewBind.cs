// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/cheat/CheatInputView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Cheat
{
    public partial class CheatInputViewBind : BaseView
    {
        public TMP_InputField cheatInput;
        public RectTransform okBtn;
        public TMP_InputField serachInput;
        public RectTransform serachBtn;
        public ScrollRect TypeScrollView;
        public RectTransform Content;
        public ScrollRect ScrollView;
        public RectTransform Content1;
        public TextMeshProUGUI time;
        public TextMeshProUGUI platform;
        public TextMeshProUGUI text;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI text_1;
        public TextMeshProUGUI codetext;
        public Image closeBtn;
        public TextMeshProUGUI extra;

        protected override void BindNodes()
        {
            EnsureBound(nameof(cheatInput), cheatInput);
            EnsureBound(nameof(okBtn), okBtn);
            EnsureBound(nameof(serachInput), serachInput);
            EnsureBound(nameof(serachBtn), serachBtn);
            EnsureBound(nameof(TypeScrollView), TypeScrollView);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(ScrollView), ScrollView);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(time), time);
            EnsureBound(nameof(platform), platform);
            EnsureBound(nameof(text), text);
            EnsureBound(nameof(nameText), nameText);
            EnsureBound(nameof(text_1), text_1);
            EnsureBound(nameof(codetext), codetext);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(extra), extra);
        }
    }
}
