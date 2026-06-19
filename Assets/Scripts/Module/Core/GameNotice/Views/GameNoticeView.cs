using Shenxiao.Generated.UI.GameNotice;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.GameNotice
{
    /// <summary>
    /// 游戏公告界面(对标老客户端 gameNotice/GameNoticeView.ts):左侧标题列表(GameNoticeListItem)+ 右侧内容(GameNoticeContentItem)。
    ///
    /// 降级:公告配置数据未移植 → 标题/内容项模板隐藏、列表空、OnShow 打 TODO。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class GameNoticeView : GameNoticeViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_GameNoticeListItem != null) _tpl_GameNoticeListItem.SetActive(false);
            if (_tpl_GameNoticeContentItem != null) _tpl_GameNoticeContentItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("GameNotice", "游戏公告打开 → 待对接 公告配置数据");
        }
    }
}
