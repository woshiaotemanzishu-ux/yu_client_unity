// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/deposit/DepositView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Deposit
{
    public partial class DepositViewBind : BaseView
    {
        public ScrollRect _list_item_con;
        public RectTransform sub_con;
        public RectTransform bottom_ui;
        public Image img_exchange;
        public Image img_record;
        public TextMeshProUGUI txt_deposit_value;
        public Image img_tip;
        public Image img_tip2;
        public TextMeshProUGUI txt1;
        public GameObject _tpl_DepositItem;
        public GameObject _tpl_DailyBottomView;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_list_item_con), _list_item_con);
            EnsureBound(nameof(sub_con), sub_con);
            EnsureBound(nameof(bottom_ui), bottom_ui);
            EnsureBound(nameof(img_exchange), img_exchange);
            EnsureBound(nameof(img_record), img_record);
            EnsureBound(nameof(txt_deposit_value), txt_deposit_value);
            EnsureBound(nameof(img_tip), img_tip);
            EnsureBound(nameof(img_tip2), img_tip2);
            EnsureBound(nameof(txt1), txt1);
            EnsureBound(nameof(_tpl_DepositItem), _tpl_DepositItem);
            EnsureBound(nameof(_tpl_DailyBottomView), _tpl_DailyBottomView);
        }
    }
}
