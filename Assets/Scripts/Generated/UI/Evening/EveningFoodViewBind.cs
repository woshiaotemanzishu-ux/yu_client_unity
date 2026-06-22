// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningFoodView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningFoodViewBind : BaseView
    {
        public ScrollRect sv;
        public RectTransform Content;
        public Image _bg_cont;
        public RectTransform _box;
        public Image img_1;
        public TextMeshProUGUI Text;
        public TextMeshProUGUI txt_exp;
        public TextMeshProUGUI txt_desc;
        public RectTransform _box2;
        public Image img_exp_buff;
        public TextMeshProUGUI txt_exp_buff;

        protected override void BindNodes()
        {
            EnsureBound(nameof(sv), sv);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_bg_cont), _bg_cont);
            EnsureBound(nameof(_box), _box);
            EnsureBound(nameof(img_1), img_1);
            EnsureBound(nameof(Text), Text);
            EnsureBound(nameof(txt_exp), txt_exp);
            EnsureBound(nameof(txt_desc), txt_desc);
            EnsureBound(nameof(_box2), _box2);
            EnsureBound(nameof(img_exp_buff), img_exp_buff);
            EnsureBound(nameof(txt_exp_buff), txt_exp_buff);
        }
    }
}
