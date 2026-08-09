using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Team;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Team
{
    /// <summary>
    /// HUD 队伍区成员行(对标老客户端 mainUI/TeamMainRoleItem.ts,自动循环 轮8)。
    /// 回填升级:结构/Bind 已由 HudTaskTeamCreator 烤好(照 MainUITaskItem 先例,业务子类直接挂在
    /// Creator 的 AddComponent 调用上,不是运行时嫁接)。由 MainUITaskTeamView.RefreshTeamPanel
    /// Instantiate(_tpl_TeamMainRoleItem) 克隆并 SetData 喂真实成员;data==null 表示空位(邀请占位)。
    ///
    /// 按钮可见性矩阵(照老端 dataChanged 逐字段对标):quitBtn(退队) = 本行是自己;
    /// pleaseLeaveBtn(请离) = 自己是队长 且 本行不是自己。
    /// </summary>
    public sealed class TeamMainRoleItem : TeamMainRoleItemBind
    {
        private TeamModel.MemberVo _vo;
        private bool _inited;

        protected override void OnInit() => EnsureInit();

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            UIUtil.AddClick(quitBtn, OnClickQuit);
            UIUtil.AddClick(pleaseLeaveBtn, OnClickKick);
            UIUtil.AddClick(non_role, OnClickInviteSlot);
        }

        /// <summary>vo==null → 空位(老端 non_role 邀请占位);否则渲染真实成员行。</summary>
        public void SetData(TeamModel.MemberVo vo)
        {
            EnsureInit();
            _vo = vo;

            if (vo == null)
            {
                if (non_role != null) non_role.gameObject.SetActive(true);
                if (role_group != null) role_group.gameObject.SetActive(false);
                if (red_point != null) red_point.gameObject.SetActive(false); // 老端本就注释掉,原样不点亮
                return;
            }

            if (non_role != null) non_role.gameObject.SetActive(false);
            if (role_group != null) role_group.gameObject.SetActive(true);

            long selfId = RoleModel.Instance.RoleId;
            bool isSelf = vo.Id == selfId;
            bool isLeader = TeamModel.Instance.IsLeaderInTeam(selfId);

            if (role_name != null) role_name.text = vo.Figure?.name ?? "";
            int level = vo.Figure?.level ?? 0;
            // 对标老端天命等级换算(level>370 显示 level-370)。
            if (this.level != null) this.level.text = (level > 370 ? level - 370 : level) + "级";
            // 对标老端 destiny_img 宽度33/0 开关(飞升标);布局归 prefab,此处只做显隐不改尺寸。
            if (destiny_img != null) destiny_img.gameObject.SetActive(level > 370);

            if (leader_tag != null) leader_tag.gameObject.SetActive(vo.TeamPosition == TeamModel.TEAM_LEADER);
            if (quitBtn != null) quitBtn.gameObject.SetActive(isSelf);
            if (pleaseLeaveBtn != null) pleaseLeaveBtn.gameObject.SetActive(isLeader && !isSelf);

            RefreshOnlineIcon(vo, selfId);
            _ = LoadHead(vo);
        }

        /// <summary>对标老端 GetOnlineStateImg:自己不显示;在线按同场景比较"附近/远离",离线灰图。
        /// 同场景或双方均为 config_scene.type==1 的野外场景时显示“附近”。</summary>
        private void RefreshOnlineIcon(TeamModel.MemberVo vo, long selfId)
        {
            if (online_img == null) return;
            if (vo.Id == selfId) { online_img.gameObject.SetActive(false); return; }

            string spriteName;
            if (vo.Online == 1)
            {
                int selfSceneId = RoleModel.Instance.SceneId;
                bool nearby = vo.SceneId == selfSceneId
                              || (MainUIConfigs.IsFieldScene(vo.SceneId) && MainUIConfigs.IsFieldScene(selfSceneId));
                spriteName = nearby ? "uirwl_013" : "uirwl_012";
            }
            else
            {
                spriteName = "uirwl_012a_1";
            }
            online_img.gameObject.SetActive(true);
            _ = ResManager.SetImageAsync(online_img, GameResPath.GetIcon("mainUI", spriteName), nativeSize: false);
        }

        private void OnClickQuit()
        {
            if (_vo == null) return;
            TeamController.Instance.QuitTeam();
        }

        private void OnClickKick()
        {
            if (_vo == null) return;
            TeamController.Instance.KickMember(_vo.Id);
        }

        private void OnClickInviteSlot()
        {
            GameLog.Info("Team", "邀请队员空位点击 → TeamInviteView 未移植,TODO");
        }

        private async System.Threading.Tasks.Task LoadHead(TeamModel.MemberVo vo)
        {
            CustomHeadItem item = await EnsureHead(role_head);
            if (item == null || _vo != vo) return;
            // 等级已由 level 文本单独展示,头像只需外观(showLevel:false);头像灰化(离线态)未接,TODO。
            item.SetRoleData(vo.Figure?.career ?? 0, vo.Figure?.turn ?? 0, vo.Figure?.level ?? 0, showLevel: false);
        }

        /// <summary>幂等:role_head 容器下已有 CustomHeadItem 直接复用,否则实例化 common/CustomHeadItem
        /// (同 Friend 模块 FriendUiUtil.EnsureHead 套路,Team 模块本地持一份,避免跨模块耦合)。</summary>
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
            CustomHeadItem item = go.GetComponent<CustomHeadItem>();
            if (item != null)
            {
                item.gameObject.SetActive(true);
                item.Show();
                item.SetActiveFrame(false);
            }
            return item;
        }
    }
}
