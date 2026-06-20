// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/redPacket/RedPacketDetailView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.RedPacket
{
    public partial class RedPacketDetailViewBind : BaseView
    {
        public Image _img_018;
        public Image _img_head;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_hasnt;
        public RectTransform _gp_money;
        public TextMeshProUGUI _lb_money;
        public Image _img_money;
        public RectTransform _Group1;
        public TextMeshProUGUI _lb_tips;
        public Image _img_tips;
        public ScrollRect _list_infos;
        public GameObject _tpl_RedPacketDetailItem;
        public GameObject _tpl_CustomHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_018), _img_018);
            EnsureBound(nameof(_img_head), _img_head);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_hasnt), _lb_hasnt);
            EnsureBound(nameof(_gp_money), _gp_money);
            EnsureBound(nameof(_lb_money), _lb_money);
            EnsureBound(nameof(_img_money), _img_money);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_list_infos), _list_infos);
            EnsureBound(nameof(_tpl_RedPacketDetailItem), _tpl_RedPacketDetailItem);
            EnsureBound(nameof(_tpl_CustomHeadItem), _tpl_CustomHeadItem);
        }
    }
}
