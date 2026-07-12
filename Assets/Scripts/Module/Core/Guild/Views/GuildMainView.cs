using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 公会信息页(对标老客户端 guild/GuildMainView.ts):公会名/公告/等级/资金/战力/成员数/会长名 + 改名/公告
    /// 入口权限门控。data-only:布局归 prefab,本类只接数据与门控逻辑。
    ///
    /// 降级:GuildBoardView(公告编辑)/GuildRenameView(改名弹窗)/main_func 功能格子(_group_item,礼包/仓库/
    /// 篝火/合并等,归属 13b 或独立子系统)本轮均未接线,权限门控本身已实现、仅 TODO 弹层本体。
    /// _btn_up_guild"升级仙宗"对标老端:老端该按钮本就只弹提示、从未真实发送 40018,本轮同样不做真实发送。
    /// </summary>
    public sealed class GuildMainView : GuildMainViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_GuildMainItem != null) _tpl_GuildMainItem.SetActive(false);
            if (_tpl_GuildMergeItem != null) _tpl_GuildMergeItem.SetActive(false);
            if (_group_item != null) _group_item.gameObject.SetActive(false); // main_func 功能格子本轮不接线

            BindClick(_btn_board, OnClickBoard);
            BindClick(btnChangeName, OnClickRename);
            BindClick(_btn_up_guild, () => TipsManager.Toast("升级仙宗"));
            BindClick(_image_5, () => TipsManager.Toast("尊贵的会长，拥有至高无上的“权利”"));
            BindClick(_image_6, () => TipsManager.Toast("仙宗等级越高，可容纳成员上限越高"));
            BindClick(_image_7, () => TipsManager.Toast("仙宗等级提升的必要资源"));
            BindClick(_image_8, () => TipsManager.Toast("仙宗成员的总战力！"));
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_GUILD_INFO_UPDATE, Refresh);
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GUILD_INFO_UPDATE, Refresh);
        }

        private void Refresh()
        {
            GuildModel.GuildInfo info = GuildModel.Instance.Info;
            if (info == null) return;

            if (_lb_guildname != null) _lb_guildname.text = info.GuildName;
            if (_lb_board != null) _lb_board.text = GuildModel.RemapAnnounce(info.Announce);
            if (_lb_level != null) _lb_level.text = "Lv." + info.GuildLv;
            if (_lb_power != null) _lb_power.text = info.CombatPower.ToString();
            if (_lb_member != null) _lb_member.text = info.MemberNum + "/" + info.MemberCapacity;
            if (_lb_mastername != null)
            {
                GuildModel.PositionEntry chief = GuildModel.Instance.GetTopMember(1);
                _lb_mastername.text = chief != null && !string.IsNullOrEmpty(chief.Name) ? chief.Name : "暂无";
            }
            if (_lb_money != null) _lb_money.text = info.Gfunds + "/" + GetUpgradeMoneyText(info.GuildLv);
        }

        /// <summary>对标老端 GetUpgradeMoney:下一级 growth_val_limit,取不到/已到顶显示"已满级"。</summary>
        private static string GetUpgradeMoneyText(int guildLv)
        {
            Newtonsoft.Json.Linq.JObject cfg = GuildConfigs.GetLv(guildLv);
            Newtonsoft.Json.Linq.JToken limit = cfg?["growth_val_limit"];
            if (limit == null || limit.Type == Newtonsoft.Json.Linq.JTokenType.Null) return "已满级";
            string s = limit.ToString();
            return string.IsNullOrEmpty(s) || s == "0" ? "已满级" : s;
        }

        private void OnClickBoard()
        {
            if (GuildModel.Instance.Info == null) return;
            if (!GuildModel.Instance.HasPermission(GuildModel.Permission.MODIFY_TENET_AND_ANNOUNCE))
            {
                TipsManager.Toast("没有修改权限");
                return;
            }
            GameLog.Info("Guild", "点击公告 → GuildBoardView 未移植(40012 数据链已通),TODO");
        }

        private void OnClickRename()
        {
            if (!GuildModel.IsGuildMaster())
            {
                TipsManager.Toast("仅会长可以修改仙宗名称");
                return;
            }
            GameLog.Info("Guild", "点击改名 → GuildRenameView 未移植(40043/44 数据链已通),TODO");
        }

        private static void BindClick(UnityEngine.Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
