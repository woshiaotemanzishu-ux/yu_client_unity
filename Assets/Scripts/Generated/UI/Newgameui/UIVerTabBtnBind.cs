// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/newgameui/UIVerTabBtn.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Newgameui
{
    public partial class UIVerTabBtnBind : BaseView
    {
        public RectTransform conta;
        public RectTransform tab;
        public Image btn_img;
        public TextMeshProUGUI tab_txt;
        public RectTransform subCon;
        public Image red_dot;
        public GameObject _tpl_UIVerTabSubBtn;

        protected override void BindNodes()
        {
            EnsureBound(nameof(conta), conta);
            EnsureBound(nameof(tab), tab);
            EnsureBound(nameof(btn_img), btn_img);
            EnsureBound(nameof(tab_txt), tab_txt);
            EnsureBound(nameof(subCon), subCon);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(_tpl_UIVerTabSubBtn), _tpl_UIVerTabSubBtn);
        }
    }
}
