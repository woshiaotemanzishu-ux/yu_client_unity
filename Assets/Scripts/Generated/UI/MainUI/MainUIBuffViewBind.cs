// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIBuffView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIBuffViewBind : BaseView
    {
        public Image image1;
        public Image image2;
        public TextMeshProUGUI label1;
        public ScrollRect _list_buff_con;
        public GameObject _tpl_MainUIBuffItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(image1), image1);
            EnsureBound(nameof(image2), image2);
            EnsureBound(nameof(label1), label1);
            EnsureBound(nameof(_list_buff_con), _list_buff_con);
            EnsureBound(nameof(_tpl_MainUIBuffItem), _tpl_MainUIBuffItem);
        }
    }
}
