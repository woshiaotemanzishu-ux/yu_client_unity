using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.InnateSkill;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Skill;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 天赋技能页(对标老客户端 innateSkill/InnateSkillView.ts)——RoleFlow "天赋" tab 内容视图(技能成长线轮3
    /// 3b 单;4转门控在 RoleFlow/BaseWindowSkinView tab 层,见 <see cref="RoleFlow"/>)。
    ///
    /// data-only:类型 tab(攻击/防守/通用/绝对,恒 4 项、type=5/6/7/8)切换按
    /// <see cref="SkillUIConfigs.GetInnateOpen"/> 门控(未开放/等级/转生不足 → toast 还原选择,不切换);
    /// 技能树按 <see cref="SkillUIConfigs.GetInnateSlots"/> 的 pos 配置定位(配置驱动的内容坐标,不算违反
    /// "布局归 prefab" 铁律——这是数据决定的技能树排布,不是我瞎摆的界面结构);选中技能 → 信息区/升级区刷新;
    /// 升级按钮走 <see cref="SkillController.LearnTalent"/>(拦截逻辑已在 <see cref="SkillTalentModel.CanLearn"/>);
    /// 重置按钮走确认框 → <see cref="SkillController.ResetTalent"/>(不做客户端拦截,对标老端);
    /// 监听 EVT_TALENT_INFO/EVT_TALENT_LEARNED 全量刷新。
    /// </summary>
    public sealed class InnateSkillView : InnateSkillViewBind
    {
        /// <summary>重置道具(对标老端 InnateSkillView.ts ref_good_ = 6200002)。</summary>
        private const int ResetGoodTypeId = 6200002;

        private List<int> _types = new List<int>();
        private InnateTypeItemRenderer[] _tabs = System.Array.Empty<InnateTypeItemRenderer>();
        private InnateListItem _listItem;
        private InnateUpInfoItem _upInfoItem;
        private InnateInfoItem _infoItem;

        private int _selectedTypeIndex;
        private int _selectedSkillId;

        protected override void OnInit()
        {
            if (_Scroller1 != null && _Scroller1.content != null)
                _listItem = _Scroller1.content.GetComponentInChildren<InnateListItem>(true);
            if (_listItem == null)
            {
                // 烤入管线不保证 ScrollRect.content 已接线(content 为空则上面静默 null):从视图根兜底找唯一实例
                _listItem = GetComponentInChildren<InnateListItem>(true);
                if (_listItem != null) GameLog.Warn("Skill", "InnateSkillView _Scroller1.content 未接线,已从根兜底定位 InnateListItem");
            }
            if (_gp_up_level != null)
                _upInfoItem = _gp_up_level.GetComponentInChildren<InnateUpInfoItem>(true);
            if (_gp_info != null)
                _infoItem = _gp_info.GetComponentInChildren<InnateInfoItem>(true);

            _types = SkillUIConfigs.GetInnateTypesSorted();
            if (_Scroller2 != null && _Scroller2.content != null)
            {
                _tabs = _Scroller2.content.GetComponentsInChildren<InnateTypeItemRenderer>(false);
            }
            if (_tabs == null || _tabs.Length == 0)
            {
                // 同 _Scroller1:content 未接线兜底(false=不含隐藏模板备份)
                _tabs = GetComponentsInChildren<InnateTypeItemRenderer>(false);
                if (_tabs.Length > 0) GameLog.Warn("Skill", "InnateSkillView _Scroller2.content 未接线,已从根兜底定位 {0} 个类型页签", _tabs.Length);
            }
            for (int i = 0; i < _tabs.Length && i < _types.Count; i++)
            {
                int type = _types[i];
                _tabs[i].SetType(type, SkillUIConfigs.GetInnateTypeName(type));
                _tabs[i].OnClicked = OnTypeTabClicked;
            }

            if (_listItem != null) _listItem.OnItemClicked = OnSkillItemClicked;

            if (_Image3 != null)
            {
                _Image3.raycastTarget = true;
                UIUtil.AddClick(_Image3, OnClickReset);
            }
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_TALENT_INFO, OnTalentInfo);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_TALENT_LEARNED, OnTalentLearned);
            if (_types.Count == 0) _types = SkillUIConfigs.GetInnateTypesSorted();
            // Bind 子组件(InnateListItem/InnateUpInfoItem)也是 BaseView:必须先 Show() 触发 EnsureBound/OnInit
            // (克隆模板捕获等),否则直接调 SetType/SetData 会在空模板上静默返回 0 条目。重复 Show 幂等无害。
            _listItem?.Show();
            _upInfoItem?.Show();
            RefreshAll();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_TALENT_INFO, OnTalentInfo);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_TALENT_LEARNED, OnTalentLearned);
        }

        private void OnTalentInfo() => RefreshAll();
        private void OnTalentLearned(int skillId, int skillLv) => RefreshAll();

        // ===================== 类型 tab =====================

        private void OnTypeTabClicked(int type)
        {
            int idx = _types.IndexOf(type);
            if (idx < 0) return;

            if (!CheckTypeOpen(type, out string reason))
            {
                TipsManager.Toast(reason);
                return; // 还原选择:压根没切换 _selectedTypeIndex/tab 视觉态
            }

            _selectedTypeIndex = idx;
            _selectedSkillId = 0; // 强制按新类型重取默认选中(对标老端 SetSelectId)
            RefreshAll();
        }

        private static bool CheckTypeOpen(int type, out string reason)
        {
            reason = null;
            SkillUIConfigs.InnateOpenCond cond = SkillUIConfigs.GetInnateOpen(type);
            if (!cond.HasCond) return true;
            if (cond.HasIsOpenFlag && !cond.IsOpen) { reason = "该天赋系暂未开放，敬请期待"; return false; }
            if (cond.HasLevelReq && RoleModel.Instance.Level < cond.OpenLv) { reason = "人物达到" + cond.OpenLv + "级后开启"; return false; }
            if (cond.HasTurnReq && (RoleModel.Instance.Figure?.turn ?? 0) < cond.Turn) { reason = "人物达到" + cond.Turn + "转后开启"; return false; }
            return true;
        }

        // ===================== 技能选中 =====================

        private void OnSkillItemClicked(int skillId)
        {
            _selectedSkillId = skillId;
            _listItem?.SetSelected(skillId);
            _upInfoItem?.SetData(skillId);
            _infoItem?.SetData(skillId);
        }

        // ===================== 重置 =====================

        private void OnClickReset()
        {
            long have = BagModel.Instance.GetTypeGoodsNum(ResetGoodTypeId);
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(ResetGoodTypeId);
            string itemName = !string.IsNullOrEmpty(basic?.Name) ? basic.Name : "重置道具";
            string text = have > 0
                ? "是否确定使用 " + itemName + " 对天赋进行重置"
                : itemName + "不足，是否确定消耗勾玉购买重置(优先消耗绑玉)";
            ConfirmDialog.Show(text, () => SkillController.Instance.ResetTalent(), null);
        }

        // ===================== 刷新 =====================

        private void RefreshAll()
        {
            if (_lb_point != null)
                _lb_point.text = SkillTalentModel.Instance.HasTalentInfo ? SkillTalentModel.Instance.LessPoint.ToString() : "-";

            for (int i = 0; i < _tabs.Length && i < _types.Count; i++)
            {
                int type = _types[i];
                int point = SkillTalentModel.Instance.GetGroup(type)?.Point ?? 0;
                _tabs[i].SetPoint(point);
                _tabs[i].SetSelected(i == _selectedTypeIndex);
            }

            if (_types.Count == 0 || _selectedTypeIndex >= _types.Count) return;
            int selType = _types[_selectedTypeIndex];

            List<SkillUIConfigs.InnateSlot> slots = SkillUIConfigs.GetInnateSlots(selType, RoleModel.Instance.Career);
            if (_selectedSkillId <= 0 && slots.Count > 0) _selectedSkillId = slots[0].SkillId;

            _listItem?.SetType(selType, _selectedSkillId);
            _upInfoItem?.SetData(_selectedSkillId);
            _infoItem?.SetData(_selectedSkillId);
        }
    }
}
