using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Role;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Skill;
using Shenxiao.Module.Core.Tasks;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 被动技能完整页面，对标老客户端 role/SkillPassiveSubItem.ts。
    /// 负责真实被动技能列表和选中详情；严格复刻老端 else if (1) 的实际展示分支，
    /// 当前页面不开放升级操作，也不会发送 21001。
    /// 720×997 页面结构及列表模板位置由 RoleModule.prefab 保存。
    /// </summary>
    public sealed class SkillPassiveSubItem : SkillPassiveSubItemBind
    {
        private const int SpecialSkillMin = 300201;
        private const int SpecialSkillMax = 300207;

        private readonly List<SkillPassiveItem> _items = new List<SkillPassiveItem>();
        private readonly HashSet<int> _runtimeItemInstanceIds = new HashSet<int>();
        private List<SkillVo> _skills = new List<SkillVo>();
        private int _selectedIndex = -1;
        private bool _eventsBound;

        protected override void OnInit()
        {
            DisableUpgradeInteraction();
        }

        protected override void OnShow(object args)
        {
            BindEvents();
            RefreshList();
        }

        protected override void OnHide()
        {
            UnbindEvents();
        }

        protected override void OnDispose()
        {
            UnbindEvents();
            ClearGeneratedItems();
        }

        private void BindEvents()
        {
            if (_eventsBound) return;
            EventDispatcher.On(GlobalEvent.EVT_SKILL_LIST_UPDATED, RefreshList);
            EventDispatcher.On<int>(GlobalEvent.EVT_SKILL_LEVEL_UP, OnLevelUp);
            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound) return;
            EventDispatcher.Off(GlobalEvent.EVT_SKILL_LIST_UPDATED, RefreshList);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SKILL_LEVEL_UP, OnLevelUp);
            _eventsBound = false;
        }

        private void OnLevelUp(int skillId)
        {
            RefreshList();
        }

        private void RefreshList()
        {
            int selectedId = _selectedIndex >= 0 && _selectedIndex < _skills.Count
                ? _skills[_selectedIndex].Id
                : 0;

            _skills = SkillPassiveItem.GetOrderedPassiveSkills();
            if (_skills.Count == 0)
            {
                _selectedIndex = -1;
                ClearGeneratedItems();
                ClearDetail();
                return;
            }

            _selectedIndex = FindSkillIndex(selectedId);
            if (_selectedIndex < 0) _selectedIndex = FindRecommendedIndex();

            BuildList();
            SelectIndex(_selectedIndex);
        }

        private int FindSkillIndex(int skillId)
        {
            if (skillId <= 0) return -1;
            for (int i = 0; i < _skills.Count; i++)
            {
                if (_skills[i].Id == skillId) return i;
            }
            return -1;
        }

        /// <summary>对标老端 RefreshIndex：首次进入优先选中材料足够升级的被动技能，否则选第一项。</summary>
        private int FindRecommendedIndex()
        {
            for (int i = 0; i < _skills.Count; i++)
            {
                SkillVo vo = _skills[i];
                if (!vo.TryGetNextLevelGoodsCost(out int typeId, out int need)) continue;
                if (BagModel.Instance.GetTypeGoodsNum(typeId) >= need) return i;
            }
            return 0;
        }

        private void BuildList()
        {
            if (_Scroller1 == null || _Scroller1.content == null)
            {
                ClearGeneratedItems();
                GameLog.Warn("Role", "被动技能列表缺少 _Scroller1 或 Content 绑定");
                return;
            }

            SkillPassiveItem template = FindItemTemplate();
            if (template == null)
            {
                ClearGeneratedItems();
                GameLog.Warn("Role", "被动技能列表缺少隐藏的 SkillPassiveItem 模板");
                return;
            }

            ClearGeneratedItems();
            template.gameObject.SetActive(false);

            for (int i = 0; i < _skills.Count; i++)
            {
                SkillPassiveItem item = Instantiate(template, _Scroller1.content, false);
                _runtimeItemInstanceIds.Add(item.GetInstanceID());
                item.gameObject.SetActive(true);

                int itemIndex = i;
                item.BindData(_skills[itemIndex], false, _ => SelectIndex(itemIndex));
                _items.Add(item);
            }
        }

        private SkillPassiveItem FindItemTemplate()
        {
            SkillPassiveItem template = FindInactiveTemplateUnder(
                _Scroller1 != null ? _Scroller1.content : null);
            if (template != null) return template;

            // 兼容模板暂时仍位于页面级 __Templates 的过渡 Prefab；正式结构优先使用 Content 内模板。
            return FindInactiveTemplateUnder(transform.root);
        }

        private SkillPassiveItem FindInactiveTemplateUnder(Transform root)
        {
            if (root == null) return null;
            SkillPassiveItem owner = GetComponentInParent<SkillPassiveItem>();
            SkillPassiveItem[] candidates = root.GetComponentsInChildren<SkillPassiveItem>(true);
            for (int i = 0; i < candidates.Length; i++)
            {
                SkillPassiveItem candidate = candidates[i];
                if (candidate == null || candidate == owner) continue;
                if (candidate.gameObject.activeSelf) continue;
                if (_items.Contains(candidate)) continue;
                if (_runtimeItemInstanceIds.Contains(candidate.GetInstanceID())) continue;
                return candidate;
            }
            return null;
        }

        private void SelectIndex(int index)
        {
            if (index < 0 || index >= _skills.Count) return;
            _selectedIndex = index;

            for (int i = 0; i < _items.Count; i++)
            {
                int itemIndex = i;
                SkillPassiveItem item = _items[itemIndex];
                if (item == null) continue;
                item.BindData(
                    _skills[itemIndex],
                    itemIndex == index,
                    _ => SelectIndex(itemIndex));
            }

            SetData(_skills[index]);
        }

        private void ClearGeneratedItems()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                SkillPassiveItem item = _items[i];
                if (item == null) continue;
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
            _items.Clear();
            _runtimeItemInstanceIds.Clear();
        }

        public void SetData(string name)
        {
            if (_lb_name != null) _lb_name.text = name ?? string.Empty;
        }

        /// <summary>绑定当前选中的真实被动技能，并严格复刻老端实际可达的展示分支。</summary>
        public void SetData(SkillVo vo)
        {
            if (vo == null)
            {
                ClearDetail();
                return;
            }

            SetData(vo.GetName());
            DisableUpgradeInteraction();

            if (_img_icon != null)
            {
                _img_icon.enabled = true;
                _ = ResManager.SetImageAsync(
                    _img_icon,
                    GameResPath.GetSkillIcon(vo.DisplayIcon),
                    nativeSize: false);
            }

            bool special = vo.Id >= SpecialSkillMin && vo.Id <= SpecialSkillMax;
            if (special)
            {
                if (lb_open != null) lb_open.text = vo.Level > 0 ? string.Empty : "(未激活)";
                if (lb_desc != null) lb_desc.text = vo.GetDesc();
                if (_lb_open3 != null) _lb_open3.text = string.Empty;
                if (_lb_desc3 != null) _lb_desc3.text = string.Empty;
                if (_img_red != null) _img_red.gameObject.SetActive(false);
                SetGroupVisible(desc1: false, desc2: true, desc3: false, expend: false, levelUp: false);
                return;
            }

            // 老端这里是 `else if (1)`，所以所有其他被动技能都进入任务解锁展示分支。
            // 解锁文案取 config_task[TaskId] 的 level/name，技能描述固定取 1 级。
            if (lb_open != null) lb_open.text = string.Empty;
            if (lb_desc != null) lb_desc.text = string.Empty;
            TaskConfigs.TaskCfg task = vo.TaskId > 0 ? TaskConfigs.Get(vo.TaskId) : null;
            if (task == null)
            {
                GameLog.Warn("Role", "被动技能缺少解锁任务配置 skill={0}, task={1}", vo.Id, vo.TaskId);
            }
            string unlockText = task == null
                ? string.Empty
                : string.Format("达到{0}级，完成{1}解锁技能", task.Level, (task.Name ?? string.Empty).Trim());
            if (vo.Level == 0) unlockText += "(未激活)";
            if (_lb_open3 != null) _lb_open3.text = unlockText;
            if (_lb_desc3 != null) _lb_desc3.text = vo.GetDesc(1);
            if (_lb_now != null) _lb_now.text = string.Empty;
            if (_lb_next != null) _lb_next.text = string.Empty;
            if (_lb_cost != null) _lb_cost.text = string.Empty;
            if (_img_cost != null) _img_cost.enabled = false;
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            SetGroupVisible(desc1: false, desc2: false, desc3: true, expend: false, levelUp: false);
        }

        private void ClearDetail()
        {
            SetData(string.Empty);
            if (_lb_now != null) _lb_now.text = string.Empty;
            if (_lb_next != null) _lb_next.text = string.Empty;
            if (lb_open != null) lb_open.text = string.Empty;
            if (lb_desc != null) lb_desc.text = string.Empty;
            if (_lb_open3 != null) _lb_open3.text = string.Empty;
            if (_lb_desc3 != null) _lb_desc3.text = string.Empty;
            if (_lb_cost != null) _lb_cost.text = string.Empty;
            if (_img_icon != null) _img_icon.enabled = false;
            if (_img_cost != null) _img_cost.enabled = false;
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            SetGroupVisible(desc1: false, desc2: false, desc3: false, expend: false, levelUp: false);
        }

        private void SetGroupVisible(bool desc1, bool desc2, bool desc3, bool expend, bool levelUp)
        {
            if (_gp_desc1 != null) _gp_desc1.gameObject.SetActive(desc1);
            if (_gp_desc2 != null) _gp_desc2.gameObject.SetActive(desc2);
            if (_gp_desc3 != null) _gp_desc3.gameObject.SetActive(desc3);
            if (_gp_expend != null) _gp_expend.gameObject.SetActive(expend);
            if (_gp_level_up != null) _gp_level_up.gameObject.SetActive(levelUp);
        }

        private void DisableUpgradeInteraction()
        {
            if (btn_bg_1 != null)
            {
                UIUtil.ClearClicks(btn_bg_1);
                btn_bg_1.raycastTarget = false;
            }
            if (_gp_level_up != null) _gp_level_up.gameObject.SetActive(false);
        }
    }
}
