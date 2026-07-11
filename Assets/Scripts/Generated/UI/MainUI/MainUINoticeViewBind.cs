// 手写(非转换器产物):MainUINoticeView 是把老端 MainUISecondaryView 里的「通知位」图标簇
// (_box_notice,右侧竞榜卡下方的开服活动/神鹤类通知图标)拆出的独立区域视图。
// 老端没有对应的 MainUINoticeView.json,故本 Bind 不由 LayaUI 转换器生成、也不会被重转覆盖;
// 字段集 = 原 MainUISecondaryViewBind 里属于 _box_notice 的那部分 + 图标克隆模板。
using UnityEngine;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUINoticeViewBind : BaseView
    {
        public RectTransform _box_notice;    // 槽位容器根:其子节点即“空槽位”,按 sibling 顺序被填图标
        public GameObject _tpl_ActivityIcon; // 活动图标克隆模板

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_notice), _box_notice);
            EnsureBound(nameof(_tpl_ActivityIcon), _tpl_ActivityIcon);
        }
    }
}
