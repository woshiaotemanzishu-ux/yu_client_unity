using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 成员列表单行(对标老客户端 guild/GuildMemberItem.ts):头像 + 名字 + 战力 + 在线/离线时长 + 头衔 +
    /// 职位图标 + 自己行专属"退出结社"按钮。头像点击接 <see cref="Shenxiao.Module.Core.LookOver.LookOverFlow"/>
    /// 资料卡(轮21 §2 PL);右键/点击头像弹出的 GuildRoleMenuView(任命/踢出菜单)老端还有其它选项,
    /// 全仓缺口仍留待补(本轮只补"查看信息"这一条最高频操作)。
    /// </summary>
    public sealed class GuildMemberItem : GuildMemberItemBind
    {
        private GuildModel.MemberEntry _data;

        protected override void OnInit()
        {
            BindClick(_btn_out, OnClickQuit);
        }

        public void SetData(GuildModel.MemberEntry data)
        {
            _data = data;
            if (data == null) return;

            bool isSelf = data.RoleId == Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId;

            if (_lb_fight != null) _lb_fight.text = data.CombatPower.ToString();
            if (_lb_name != null) _lb_name.text = data.Name;
            if (_btn_out != null) _btn_out.gameObject.SetActive(isSelf);

            if (_lb_time != null)
            {
                if (data.Online)
                {
                    _lb_time.color = new Color(0.35f, 0.85f, 0.4f);
                    _lb_time.text = "在线";
                }
                else
                {
                    _lb_time.color = new Color(0.6f, 0.6f, 0.6f);
                    _lb_time.text = FormatElapsed(data.OfflineTime);
                }
            }

            if (_lb_title != null)
            {
                Newtonsoft.Json.Linq.JObject cfg = ShopConfigs.GetGuildPrestige(data.TitleId);
                string name = cfg?["title_name"]?.ToString();
                _lb_title.text = !string.IsNullOrEmpty(name) ? name.Trim() : "萌新";
            }

            SetPositionIcon(data.Position);
            _ = LoadHead(data);
        }

        /// <summary>对标老端 GuildMemberItem.SetDate 的职位图标分支(会长/副会长/宝贝/精英四档,会员无图标)。</summary>
        private void SetPositionIcon(int position)
        {
            if (_img_pos == null) return;
            string source = position switch
            {
                1 => "uigh_029a",
                2 => "uigh_029b",
                4 => "uigh_029c",
                5 => "uigh_029d",
                _ => null,
            };
            if (source == null)
            {
                _img_pos.gameObject.SetActive(false);
                return;
            }
            _img_pos.gameObject.SetActive(true);
            _ = ResManager.SetImageAsync(_img_pos, GameResPath.GetIcon("guild", source), nativeSize: false);
        }

        private async System.Threading.Tasks.Task LoadHead(GuildModel.MemberEntry data)
        {
            CustomHeadItem item = await EnsureHead(_playerHead);
            if (item == null || _data != data) return;
            item.SetRoleData(data.Career, data.Turn, data.Level, showLevel: true);
            long roleId = data.RoleId;
            item.SetClickFunc(() => Shenxiao.Module.Core.LookOver.LookOverFlow.Show(roleId));
        }

        /// <summary>幂等:_playerHead 容器下已有 CustomHeadItem 直接复用,否则实例化(同 Team/Friend 模块套路,
        /// 本模块本地持一份避免跨模块耦合)。</summary>
        private static async System.Threading.Tasks.Task<CustomHeadItem> EnsureHead(RectTransform container)
        {
            if (container == null) return null;
            CustomHeadItem existing = container.GetComponentInChildren<CustomHeadItem>(true);
            if (existing != null) return existing;

            GameObject go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "CustomHeadItem"), container);
            if (go == null) return null;
            go.name = "CustomHeadItem";
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            return go.GetComponent<CustomHeadItem>();
        }

        private void OnClickQuit()
        {
            ConfirmDialog.Show(
                "退出结社后<font color=#60aeff>结社技能等级</font>、<font color=#ff9015>结社头衔</font>将保留，" +
                "但<font color=#a376ff>结社贡献</font>、<font color=#ff9015>仓库积分</font>将被清空。是否确定退出结社？",
                () => GuildController.Instance.Quit(),
                null);
        }

        /// <summary>对标老端 TimeUtil.GetTimeStrByServerTime——40006 的 offline_time 字段实为"离线已过去的秒数"
        /// (非绝对时间戳:老端调用点 GetTimeStrByServerTime(getServerTime()-offline_time) 内部又做一次
        /// getServerTime()-time 相减,两次相减抵消后净效果=直接格式化 offline_time 本身),此处直接格式化。</summary>
        private static string FormatElapsed(long sec)
        {
            if (sec <= 0) return "刚刚";
            long month = sec / (3600L * 24 * 30);
            long rem = sec % (3600L * 24 * 30);
            long day = rem / (3600L * 24);
            rem %= 3600L * 24;
            long hour = rem / 3600L;
            rem %= 3600L;
            long minute = rem / 60L;
            long second = rem % 60L;
            if (month != 0) return month + "月前";
            if (day != 0) return day + "天前";
            if (hour != 0) return hour + "小时前";
            if (minute != 0) return minute + "分钟前";
            if (second != 0) return second + "秒前";
            return "刚刚";
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
