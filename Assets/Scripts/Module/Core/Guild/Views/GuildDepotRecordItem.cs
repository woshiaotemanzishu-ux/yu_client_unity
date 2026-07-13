using Shenxiao.Generated.UI.Guild;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 仓库兑换记录单行(对标老客户端 guild/GuildDepotRecordItem.ts):"[MM-DD HH.mm] {角色名/仙宗仓管} {捐献/兑换/清理}"
    /// + 物品名(euqip)。</summary>
    public sealed class GuildDepotRecordItem : GuildDepotRecordItemBind
    {
        public void SetData(GuildModel.DepotRecordEntry data)
        {
            if (data == null) return;
            System.DateTime time = System.DateTimeOffset.FromUnixTimeSeconds(data.Time).ToLocalTime().DateTime;
            string roleName = string.IsNullOrEmpty(data.RoleName) ? "仙宗仓管" : data.RoleName;
            string typeText = data.ExchangeType switch
            {
                1 => "捐献",
                2 => "兑换",
                3 => string.IsNullOrEmpty(data.RoleName) ? "自动清理" : "清理",
                _ => "",
            };
            if (_lb_content != null) _lb_content.text = string.Format("[{0:MM-dd HH.mm}] {1} {2}", time, roleName, typeText);
            if (euqip != null) euqip.text = GoodsModel.GetGoodsName(data.TypeId);
        }
    }
}
