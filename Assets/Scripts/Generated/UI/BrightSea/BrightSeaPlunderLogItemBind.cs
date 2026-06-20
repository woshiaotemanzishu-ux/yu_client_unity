// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/brightSea/BrightSeaPlunderLogItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BrightSea
{
    public partial class BrightSeaPlunderLogItemBind : BaseView
    {
        public Image _img_line;
        public Image _selected;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_level;
        public Image _tm_img;
        public TextMeshProUGUI _lb_guild;
        public TextMeshProUGUI _lb_ship;
        public TextMeshProUGUI _lb_fight;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_line), _img_line);
            EnsureBound(nameof(_selected), _selected);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_level), _lb_level);
            EnsureBound(nameof(_tm_img), _tm_img);
            EnsureBound(nameof(_lb_guild), _lb_guild);
            EnsureBound(nameof(_lb_ship), _lb_ship);
            EnsureBound(nameof(_lb_fight), _lb_fight);
        }
    }
}
