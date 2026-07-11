// 手写(非转换器产物):MainUIRankView 是把老端 MainUIActivityView 里的「竞榜 / 头号玩家榜」卡片
// (_box_rank 子树)从 MainUIActivityView 拆出的独立区域视图。老端没有对应的 MainUIRankView.json,
// 故本 Bind 不由 LayaUI 转换器生成、也不会被重转覆盖;字段集 = 原 MainUIActivityViewBind 里属于
// _box_rank 的那部分(_tpl_EquipmentItem 原本就未烤进 prefab、且业务改走按址加载,故不保留;
// _tpl_TopPlayerTipItem 气泡模板业务从未接线,已连同 __Templates 挂载点一起移除 —— 将来要用时
// 按址加载独立的 TopPlayerTipItem.prefab,同 EquipmentItem 套路)。
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIRankViewBind : BaseView
    {
        public RectTransform _box_rank;
        public Image _img_bg;
        public RectTransform effect;
        public RectTransform icon_box;
        public RectTransform model_gp;
        public Image icon;
        public Image bg1;
        public TextMeshProUGUI name;
        public TextMeshProUGUI player_name;
        public TextMeshProUGUI time;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_rank), _box_rank);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(effect), effect);
            EnsureBound(nameof(icon_box), icon_box);
            EnsureBound(nameof(model_gp), model_gp);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(name), name);
            EnsureBound(nameof(player_name), player_name);
            EnsureBound(nameof(time), time);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
