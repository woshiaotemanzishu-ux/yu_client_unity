using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Skill;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 技能条(对标老客户端 MainUISkillView.ts)。
    ///
    /// 数据真源:监听 EVT_SKILL_LIST_UPDATED / EVT_SKILL_BAR_UPDATED → 读 SkillManager.ShortcutList 铺 4 槽
    /// (对标 InitEvent 绑 UPDATE_SKILL_LIST → UpdateView);技能 id/等级/图标来自 21002 + config_skill,不硬编码。
    /// 4 槽按老端固定坐标 [4,99] [39,64] [96,63] [132,101] 摆放(Laya→uGUI 由 MainUISkillItem.SetPosition 转 y)。
    ///
    /// 自动战斗按钮:UpdateAutoFightState 据 AutoFightModel 切皮肤(非自动 uizjmgj_003a / 自动 uizjmgj_001b /
    /// 临时模式 uizjmgj_001a1);点击 ResponseAutoBtnClick → AutoFightModel.Toggle(对标 SetAutoFight)。
    /// 注意:这是自动战斗(AutoFight),不是自动闯关(AutoBrush,见 MainUIAutoBrushView),两者分属不同 Model。
    ///
    /// 伙伴技能锁:伙伴系统/PartnerAwakeModel 未移植 → 本轮只把 _img_partner_lock 点击接到边界(打开 PartnerAwakeShowView
    /// 未移植),不伪造 func-open/觉醒条件;锁显隐保持转换产物默认(差异见报告)。
    /// </summary>
    public sealed class MainUISkillView : MainUISkillViewBind
    {
        // 老端固定坐标布局(MainUISkillView.ts UpdateView fixedPositions)。
        private static readonly Vector2[] FixedPositions =
        {
            new Vector2(4, 99),
            new Vector2(39, 64),
            new Vector2(96, 63),
            new Vector2(132, 101),
        };

        private const string AUTO_FIGHT_OFF = "uizjmgj_003a";
        private const string AUTO_FIGHT_ON = "uizjmgj_001b";
        private const string AUTO_FIGHT_TEMP = "uizjmgj_001a1";

        private readonly List<MainUISkillItem> _items = new List<MainUISkillItem>();
        private bool _clickBound;

        protected override void OnInit()
        {
            if (_tpl_MainUISkillItem != null) _tpl_MainUISkillItem.SetActive(false);
            if (_tpl_CirCleCdView != null) _tpl_CirCleCdView.SetActive(false);

            BindButtons();

            EventDispatcher.On(GlobalEvent.EVT_SKILL_LIST_UPDATED, OnSkillUpdated);
            EventDispatcher.On(GlobalEvent.EVT_SKILL_BAR_UPDATED, OnSkillUpdated);
            EventDispatcher.On<bool>(GlobalEvent.EVT_AUTO_FIGHT_STATE, OnAutoFightState);

            RefreshAll();
        }

        protected override void OnShow(object args)
        {
            RefreshAll();
        }

        protected override void OnDispose()
        {
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void UnbindEvents()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SKILL_LIST_UPDATED, OnSkillUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_SKILL_BAR_UPDATED, OnSkillUpdated);
            EventDispatcher.Off<bool>(GlobalEvent.EVT_AUTO_FIGHT_STATE, OnAutoFightState);
        }

        private void RefreshAll()
        {
            UpdateAutoFightState();
            RefreshSkills();
            InitPartnerSkill();
        }

        private void OnSkillUpdated() => RefreshSkills();
        private void OnAutoFightState(bool _) => UpdateAutoFightState();

        // ===================== 技能 4 槽(对标 UpdateView)=====================

        /// <summary>按 SkillManager.ShortcutList 铺技能槽(对标 UpdateView:固定坐标 + SetData)。</summary>
        public void RefreshSkills()
        {
            IList<SkillVo> skills = SkillManager.Instance.ShortcutList;
            int count = skills?.Count ?? 0;
            if (count > FixedPositions.Length)
            {
                GameLog.Warn("MainUI", "shortcutList={0} 超过固定槽位 {1},只铺前 {1}(差异记录:>4 槽暂不扩)", count, FixedPositions.Length);
                count = FixedPositions.Length;
            }

            for (int i = 0; i < count; i++)
            {
                MainUISkillItem item = GetOrCreateItem(i);
                if (item == null) continue;
                item.gameObject.SetActive(true);
                item.SetData(skills[i]);
                item.SetPosition(FixedPositions[i].x, FixedPositions[i].y);
            }
            for (int i = count; i < _items.Count; i++)
            {
                if (_items[i] != null) _items[i].gameObject.SetActive(false);
            }

            if (count == 0)
            {
                GameLog.Info("MainUI", "技能槽为空(等 21002 回包或 config 未就绪)");
            }
        }

        private MainUISkillItem GetOrCreateItem(int index)
        {
            while (_items.Count <= index) _items.Add(null);
            if (_items[index] != null) return _items[index];

            if (_tpl_MainUISkillItem == null || _box_skill_con == null)
            {
                GameLog.Error("MainUI", "MainUISkillView 缺 _tpl_MainUISkillItem 或 _box_skill_con");
                return null;
            }

            GameObject go = Instantiate(_tpl_MainUISkillItem, _box_skill_con);
            go.SetActive(true);

            MainUISkillItem item = go.GetComponent<MainUISkillItem>();
            if (item == null)
            {
                GameLog.Error("MainUI", "_tpl_MainUISkillItem 缺 MainUISkillItem 组件(回填?)");
                Destroy(go);
                return null;
            }

            _items[index] = item;
            return item;
        }

        // ===================== 自动战斗按钮(对标 UpdateAutoFightState / ResponseAutoBtnClick)=====================

        public void UpdateAutoFightState()
        {
            if (_img_auto_fight == null) return;
            AutoFightModel model = AutoFightModel.Instance;
            string source = model.AutoFightState ? AUTO_FIGHT_ON : AUTO_FIGHT_OFF;
            if (model.AutoFightState && model.TempMode) source = AUTO_FIGHT_TEMP;
            _ = ResManager.SetImageAsync(_img_auto_fight, GameResPath.GetIcon("mainUI", source), nativeSize: false);
        }

        private void ResponseAutoBtnClick()
        {
            // 老端此处先判经验副本(IsExpScene → Message『副本自动战斗中』);场景系统该状态未移植,本轮直接切换并记录。
            bool on = AutoFightModel.Instance.Toggle();
            GameLog.Info("MainUI", "自动战斗按钮点击 → {0}(对标 ResponseAutoBtnClick→SetAutoFight)", on ? "开启" : "取消");
        }

        // ===================== 伙伴技能锁(对标 InitPartnerSkill 边界)=====================

        private void InitPartnerSkill()
        {
            // 伙伴系统/PartnerModel/PartnerAwakeModel 未移植:不创建伙伴技能位,不伪造 func-open/觉醒条件。
            // _img_partner_lock 显隐保持转换产物默认;点击接到边界(打开 PartnerAwakeShowView 待下一轮)。
        }

        private void BindButtons()
        {
            if (_clickBound) return;
            if (_img_auto_fight != null) UIUtil.AddClick(_img_auto_fight, ResponseAutoBtnClick);
            if (_img_partner_lock != null) UIUtil.AddClick(_img_partner_lock, OnClickPartnerLock);
            _clickBound = true;
        }

        private void OnClickPartnerLock()
        {
            GameLog.Info("MainUI", "点击伙伴技能锁 → 打开 PartnerAwakeShowView 未移植(伙伴系统下一轮)");
        }
    }
}
