using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Guild;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 公会主界面功能格子单元(对标老客户端 guild/GuildMainItem.ts,ConfigGuild.json.main_func 驱动):
    /// 图标 + 名称 + 点击回调。本轮仅"结社仓库"(id=4)真接线,其余入口仍 TODO(见 <see cref="GuildMainView"/>)。
    /// </summary>
    public sealed class GuildMainItem : GuildMainItemBind
    {
        protected override void OnInit()
        {
            if (_reddot != null) _reddot.gameObject.SetActive(false); // 本轮无统一红点系统
        }

        public void SetData(int id, string title, string icon, System.Action onClick)
        {
            if (_lb_name != null) _lb_name.text = title;
            if (_img_icon != null) _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetIcon("guild", icon), false, false);
            if (_img_icon != null)
            {
                _img_icon.raycastTarget = true;
                UIUtil.AddClick(_img_icon, () => onClick?.Invoke());
            }
        }
    }
}
