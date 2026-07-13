using Shenxiao.Generated.UI.Guild;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 宝箱日志单行(对标老客户端 guild/GuildRBRecordItem.ts):"【HH:mm:ss】{角色名}激活了{任务名}宝箱"。
    /// </summary>
    public sealed class GuildRBRecordItem : GuildRBRecordItemBind
    {
        public void SetData(GuildModel.BoxLogEntry data)
        {
            if (data == null || _lb_desc == null) return;
            string title = GuildConfigs.GetDailyTask(data.TaskId)?["name"]?.ToString() ?? "";
            System.DateTime time = System.DateTimeOffset.FromUnixTimeSeconds(data.Time).ToLocalTime().DateTime;
            _lb_desc.text = string.Format("【{0:HH:mm:ss}】{1}激活了{2}宝箱", time, data.RoleName, title);
        }
    }
}
