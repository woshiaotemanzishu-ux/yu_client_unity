// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIActivityView.json
// ⚠️ 手动裁剪 + Unity 化:
//  1) 老端 MainUIActivityView.json 里的 _box_rank 竞榜卡子树已拆到独立视图 MainUIRankView(见 MainUIRankViewBind)。
//  2) 图标网格改为【槽位式】:_gp_con 下由美术手摆一批空槽位,运行时把图标按顺序填进各槽(见 MainUIActivityView)。
//     不再分 _group_* / 不再用布局组;代码绝不算坐标,槽位位置全在 prefab。
//  若重跑转换器覆盖本文件,记得重新按此裁剪。
using UnityEngine;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIActivityViewBind : BaseView
    {
        public RectTransform _gp_con;            // 槽位容器根:其子节点即"空槽位",按 sibling 顺序被填图标
        public GameObject _tpl_ActivityIcon;     // 活动图标克隆模板

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_tpl_ActivityIcon), _tpl_ActivityIcon);
        }
    }
}
