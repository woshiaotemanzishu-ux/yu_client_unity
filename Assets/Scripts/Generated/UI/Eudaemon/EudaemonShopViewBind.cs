// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eudaemon/EudaemonShopView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eudaemon
{
    public partial class EudaemonShopViewBind : BaseView
    {
        public ScrollRect _list_shop;
        public Image _img_bg;
        public Image _img_bg4;
        public Image _img_bg2;
        public Image _img_bg3;
        public Image _img_bg5;
        public TextMeshProUGUI _lb_money;
        public Image _img_money_icon;
        public Image _img_add;
        public RectTransform _box_task;
        public TextMeshProUGUI _html_time;
        public GameObject _tpl_EudaemonShopBigItem;
        public GameObject _tpl_EudaemonShopItem;
        public GameObject _tpl_EudaemonShopTaskItem;
        public GameObject _tpl_EudaemonTaskTabItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_list_shop), _list_shop);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg4), _img_bg4);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_img_bg5), _img_bg5);
            EnsureBound(nameof(_lb_money), _lb_money);
            EnsureBound(nameof(_img_money_icon), _img_money_icon);
            EnsureBound(nameof(_img_add), _img_add);
            EnsureBound(nameof(_box_task), _box_task);
            EnsureBound(nameof(_html_time), _html_time);
            EnsureBound(nameof(_tpl_EudaemonShopBigItem), _tpl_EudaemonShopBigItem);
            EnsureBound(nameof(_tpl_EudaemonShopItem), _tpl_EudaemonShopItem);
            EnsureBound(nameof(_tpl_EudaemonShopTaskItem), _tpl_EudaemonShopTaskItem);
            EnsureBound(nameof(_tpl_EudaemonTaskTabItem), _tpl_EudaemonTaskTabItem);
        }
    }
}
