using Shenxiao.Generated.UI.MainUI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 采集进度条。对标老客户端 CollectBarView.ts:它不在 InitMainUI 首批打开之列,
    /// 而是采集事件触发时按需 new+Open(MainUIController.ts:533-537),开启后也先
    /// display_obj.visible=false,直到 StartCollect 才显示进度(:78-105、:352-362)。
    ///
    /// 因此本视图「初始隐藏」由 MainUIFlow 统一关闭所有子视图、且不把它列入首批 Show
    /// 来保证;不在 OnInit 里 SetActive(false)——那会与该视图后续被采集事件 Show 冲突。
    /// 进度遮罩绘制(Laya.Sprite mask)是运行期采集逻辑,后续接采集链路时再补,本轮不造。
    /// </summary>
    public sealed class CollectBarView : CollectBarViewBind
    {
    }
}
