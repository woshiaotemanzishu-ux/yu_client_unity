using System.Collections.Generic;
using Shenxiao.Generated.UI.Role;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 主动技能详情页(对标老客户端 role/SkillInitiativeSubItem.ts)。
    /// 六个图标按老端固定坐标放入 Prefab 预留的 _group_item，所有内容来自真实技能配置和协议数据。
    /// </summary>
    public sealed class SkillInitiativeSubItem : SkillInitiativeSubItemBind
    {
        private static readonly Vector2[] ItemPositions =
        {
            new Vector2(17f, -221f),
            new Vector2(91f, -339f),
            new Vector2(251f, -376f),
            new Vector2(412f, -345f),
            new Vector2(522f, -226f),
            new Vector2(565f, -83f),
        };

        private readonly List<SkillInitiativeItem> _items = new List<SkillInitiativeItem>();
        private SkillVo _selected;

        protected override void OnInit()
        {
            ApplyForbiddenSkillEntryState();
            SetCareerImage();

            // 模板只作为运行时克隆源，不让它作为第七个可见技能槽。
            if (_tpl_SkillInitiativeItem != null) _tpl_SkillInitiativeItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            ApplyForbiddenSkillEntryState();
            EventDispatcher.On(GlobalEvent.EVT_SKILL_LIST_UPDATED, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_SKILL_BAR_UPDATED, Refresh);
            EventDispatcher.On<int>(GlobalEvent.EVT_SKILL_LEVEL_UP, OnLevelUp);
            SetCareerImage();
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SKILL_LIST_UPDATED, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_SKILL_BAR_UPDATED, Refresh);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SKILL_LEVEL_UP, OnLevelUp);
        }

        private void OnLevelUp(int skillId) => Refresh();

        private void Refresh()
        {
            ClearItems();
            if (_tpl_SkillInitiativeItem == null || _group_item == null)
            {
                GameLog.Warn("Role", "主动技能页缺少 _tpl_SkillInitiativeItem 或 _group_item，已清空详情");
                ClearDetails();
                return;
            }

            List<SkillVo> skills = BuildSkillList();

            if (skills.Count == 0)
            {
                ClearDetails();
                return;
            }

            for (int i = 0; i < skills.Count && i < ItemPositions.Length; i++)
            {
                SkillVo vo = skills[i];
                GameObject instance = Instantiate(_tpl_SkillInitiativeItem, _group_item, false);
                instance.name = "SkillInitiativeItem_" + i;
                instance.SetActive(false);

                SkillInitiativeItem item = instance.GetComponent<SkillInitiativeItem>();
                if (item == null)
                {
                    Destroy(instance);
                    continue;
                }

                RectTransform rect = instance.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = ItemPositions[i];
                }

                SkillVo boundVo = vo;
                SkillInitiativeItem boundItem = item;
                _items.Add(boundItem);
                item.Show();
                item.SetClick(() => Select(boundItem, boundVo));
                item.SetData(boundVo);
                item.SetSelected(false);
            }

            if (_items.Count == 0)
            {
                ClearDetails();
                return;
            }

            _selected = skills[0];
            for (int i = 0; i < _items.Count; i++) _items[i].SetSelected(i == 0);
            UpdateDetails(_selected);
        }

        private List<SkillVo> BuildSkillList()
        {
            var result = new List<SkillVo>();
            int career = RoleModel.Instance.Career;
            List<SkillUIConfigs.CareerSkill> configured = SkillUIConfigs.GetCareerSkills(career);

            // 老端主动技能页按 ConfigSkillUI 的六个槽展示，未学技能保留为 level=0 锁定槽。
            for (int i = 0; i < configured.Count && result.Count < ItemPositions.Length; i++)
            {
                int skillId = configured[i].SkillId;
                if (skillId <= 0) continue;
                SkillVo vo;
                if (SkillConfigs.GetSkillType(skillId) == 2)
                {
                    // 老端主动页对被动型槽位使用 DungeonModel 心法等级，不能复用 21002 的共享 SkillVo。
                    vo = new SkillVo(skillId)
                    {
                        Level = DungeonModel.Instance.GetHeartSkillLevel((uint)skillId),
                    };
                }
                else
                {
                    // 普通主动技能继续沿用 21002 权威等级。
                    vo = SkillManager.Instance.GetSkill(skillId) ?? new SkillVo(skillId);
                }
                result.Add(vo);
            }

            // 配置尚未完成加载时保留真实协议列表，避免空白页；仍然最多六项。
            if (result.Count == 0 && SkillManager.Instance.ShortcutList != null)
            {
                for (int i = 0; i < SkillManager.Instance.ShortcutList.Count && result.Count < ItemPositions.Length; i++)
                {
                    SkillVo vo = SkillManager.Instance.ShortcutList[i];
                    if (vo != null) result.Add(vo);
                }
            }

            return result;
        }

        private void Select(SkillInitiativeItem selectedItem, SkillVo vo)
        {
            if (vo == null) return;
            _selected = vo;
            for (int i = 0; i < _items.Count; i++)
            {
                SkillInitiativeItem item = _items[i];
                item.SetSelected(item == selectedItem);
            }
            UpdateDetails(vo);
        }

        private void UpdateDetails(SkillVo vo)
        {
            if (vo == null)
            {
                ClearDetails();
                return;
            }

            if (_lb_name != null) _lb_name.text = vo.GetName();
            if (_lb_level != null) _lb_level.text = vo.Locked ? "[未解锁]" : $"[{vo.Level}级]";
            if (_lb_content_now != null) _lb_content_now.text = vo.Level > 0 ? vo.GetDesc() : "暂未学习";
            if (_lb_content_next != null) _lb_content_next.text = vo.IsMaxLevel ? "已满级" : vo.GetDesc(vo.Level + 1);
            if (_img_max != null) _img_max.gameObject.SetActive(vo.IsMaxLevel);
        }

        private void ClearDetails()
        {
            _selected = null;
            if (_lb_name != null) _lb_name.text = "";
            if (_lb_level != null) _lb_level.text = "";
            if (_lb_content_now != null) _lb_content_now.text = "";
            if (_lb_content_next != null) _lb_content_next.text = "";
            if (_img_max != null) _img_max.gameObject.SetActive(false);
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == null) continue;
                // Destroy 在帧尾才生效；页签被重复点击时若不先隐藏，旧六格与新六格会同帧叠成 12 个射线目标。
                _items[i].gameObject.SetActive(false);
                Destroy(_items[i].gameObject);
            }
            _items.Clear();
        }

        private void SetCareerImage()
        {
            if (_img_boy == null) return;
            _img_boy.enabled = true;
            RectTransform rect = _img_boy.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            int career = RoleModel.Instance.Career;
            string address = career == 1
                ? "resource/game/role/other/uijn_001_character"
                : GameResPath.GetIconOtherPath("role", "uijn_00" + career);
            _ = ResManager.SetImageAsync(_img_boy, address, nativeSize: true);
        }

        private void ApplyForbiddenSkillEntryState()
        {
            if (skill_wear_btn == null) return;

            // 对标老端当前可达链：forbbiden_skill_info 恒为 null，21101-21104 已正式 KILL，
            // ForbiddenSkillBtnVisible 因而始终隐藏本入口；不得展示一个只写日志的伪“技能装配”按钮。
            skill_wear_btn.raycastTarget = false;
            skill_wear_btn.gameObject.SetActive(false);
        }
    }
}
