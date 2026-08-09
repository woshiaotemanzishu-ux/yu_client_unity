using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Team;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Team
{
    /// <summary>
    /// 组队大厅队伍行。只消费 24012 的权威大厅快照；本轮不新增任何写事务点击绑定。
    /// 头像点击不绑定人物菜单：老端 ShowPlayerMenu 为空且 24011 已按死客户端流固化为 killlist。
    /// </summary>
    public sealed class TeamHallItem : TeamHallItemBind
    {
        private TeamModel.MemberVo _leader;
        private System.Threading.Tasks.Task<CustomHeadItem> _headLoadTask;
        private int _renderVersion;

        public void SetData(TeamModel.HallEntryVo vo)
        {
            int renderVersion = ++_renderVersion;
            _leader = FindLeader(vo);

            if (role_num != null)
                role_num.text = vo == null ? string.Empty : $"人数：{vo.Num}/{TeamModel.TEAMER_MAX}";

            if (_leader == null)
            {
                if (role_name != null) role_name.text = string.Empty;
                if (level != null) level.text = string.Empty;
                if (online_state != null) online_state.text = string.Empty;
                if (leader_tag != null) leader_tag.gameObject.SetActive(false);
                HideHead();
                return;
            }

            if (role_name != null) role_name.text = _leader.Figure?.name ?? string.Empty;
            int roleLevel = _leader.Figure?.level ?? 0;
            if (level != null)
                level.text = roleLevel > 370 ? $"神创{roleLevel - 370}级" : $"{roleLevel}级";
            if (leader_tag != null) leader_tag.gameObject.SetActive(true);
            RefreshOnlineState(_leader);
            HideHead();
            _ = LoadHead(_leader, renderVersion);
        }

        private static TeamModel.MemberVo FindLeader(TeamModel.HallEntryVo vo)
        {
            if (vo?.Members == null) return null;
            foreach (TeamModel.MemberVo member in vo.Members)
            {
                if (member != null && member.TeamPosition == TeamModel.TEAM_LEADER)
                    return member;
            }
            return null;
        }

        private void RefreshOnlineState(TeamModel.MemberVo member)
        {
            if (online_state == null) return;
            if (member.Id == RoleModel.Instance.RoleId)
            {
                online_state.text = string.Empty;
                return;
            }

            if (member.Online != 1)
                online_state.text = "<color=#9c9c9c>离线</color>";
            else if (IsNearbyScene(member.SceneId, RoleModel.Instance.SceneId))
                online_state.text = "<color=#0a953e>附近</color>";
            else
                online_state.text = "<color=#ff4f50>远离</color>";
        }

        private static bool IsNearbyScene(int memberSceneId, int selfSceneId)
        {
            return memberSceneId == selfSceneId
                   || (MainUIConfigs.IsFieldScene(memberSceneId) && MainUIConfigs.IsFieldScene(selfSceneId));
        }

        private void HideHead()
        {
            if (role_head == null) return;
            CustomHeadItem item = role_head.GetComponentInChildren<CustomHeadItem>(true);
            if (item != null) item.gameObject.SetActive(false);
        }

        private async System.Threading.Tasks.Task LoadHead(TeamModel.MemberVo member, int renderVersion)
        {
            if (_headLoadTask == null) _headLoadTask = EnsureHead(role_head);
            CustomHeadItem item = await _headLoadTask;
            if (item == null) return;
            if (_renderVersion != renderVersion || _leader != member)
            {
                if (_leader == null) item.gameObject.SetActive(false);
                return;
            }
            item.gameObject.SetActive(true);
            item.Show();
            item.SetActiveFrame(false);
            item.SetRoleData(member.Figure?.career ?? 0, member.Figure?.turn ?? 0, member.Figure?.level ?? 0, showLevel: false);
        }

        private static async System.Threading.Tasks.Task<CustomHeadItem> EnsureHead(RectTransform container)
        {
            if (container == null) return null;
            CustomHeadItem existing = container.GetComponentInChildren<CustomHeadItem>(true);
            if (existing != null) return existing;

            GameObject go = await ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab("common", "CustomHeadItem"), container);
            if (go == null) return null;
            go.name = "CustomHeadItem";
            if (go.transform is RectTransform rt)
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
                item.gameObject.SetActive(false);
                item.SetActiveFrame(false);
            }
            return item;
        }
    }
}
