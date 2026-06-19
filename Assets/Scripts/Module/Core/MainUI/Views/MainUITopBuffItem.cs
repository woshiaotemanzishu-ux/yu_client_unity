using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 顶部 Buff 指示项(对标老客户端 MainUITopBuffItem.ts):顶部状态栏一行的 Buff 图标,带 CD 倒计时 + 圆形遮罩。
    ///
    /// 降级:Buff 数据源(MainUIModel buff/skill_key、bufficon 配置、服务器时间 CD、圆形 mask pie 动画)未移植 →
    /// 用最小 DTO <see cref="TopBuffData"/> 填图标 + 可选 CD 文本;CD 圆形遮罩 _img_mask 先隐藏
    /// (Laya.Sprite drawPie 无直接对应,待用 Image radial fill 补)。父 MainUITopView 在 Buff 数据接上后
    /// 克隆本项并 SetData。
    /// </summary>
    public sealed class MainUITopBuffItem : MainUITopBuffItemBind
    {
        /// <summary>由父 MainUITopView 克隆后调用,填本项 Buff 图标 + CD 文本。</summary>
        public void SetData(TopBuffData data)
        {
            if (data == null) return;

            if (_img_bg != null && !string.IsNullOrEmpty(data.Icon))
                _ = ResManager.SetImageAsync(_img_bg, GameResPath.GetIcon("bufficon", data.Icon), nativeSize: false);

            if (_lb_time != null) _lb_time.text = data.TimeText ?? "";

            // CD 圆形遮罩依赖服务器时间 + pie 动画(未移植)→ 先隐藏,不伪造。
            if (_img_mask != null) _img_mask.gameObject.SetActive(false);
        }
    }

    /// <summary>顶部 Buff 项最小数据(待 MainUIModel buff + bufficon 配置 + 服务器时间移植后填充)。</summary>
    public sealed class TopBuffData
    {
        public string Icon;
        public string TimeText;
    }
}
