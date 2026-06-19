using System.Collections.Generic;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 技能条(对标老客户端 MainUISkillView.ts)。
    /// 自动战斗按钮非自动默认图(UpdateAutoFightState:非自动取 "uizjmgj_003a")。
    /// 技能按钮网格:克隆 _tpl_MainUISkillItem 到 _box_skill_con,逐个 SetData(对标老端建项)。
    /// 降级:SkillManager/技能列表/CirCleCdView 圆形 CD 未移植 → 模板隐藏、列表暂空 + 日志「待对接技能数据」;
    /// SkillManager 移植后订阅技能更新 → RefreshSkills(skillVos)。
    /// </summary>
    public sealed class MainUISkillView : MainUISkillViewBind
    {
        private readonly List<MainUISkillItem> _items = new List<MainUISkillItem>();

        protected override void OnInit()
        {
            // 老客户端 SetImageSprite 使用 mainUI_asset 图集名;Unity 已拆成 mainUI/texture 散图地址。
            _ = ResManager.SetImageAsync(_img_auto_fight, GameResPath.GetIcon("mainUI", "uizjmgj_003a"), nativeSize: false);

            if (_tpl_MainUISkillItem != null) _tpl_MainUISkillItem.SetActive(false);
            if (_tpl_CirCleCdView != null) _tpl_CirCleCdView.SetActive(false);
            // TODO 待接:SkillManager 移植后订阅技能列表更新 → RefreshSkills(skillVos)。
        }

        /// <summary>按技能列表铺技能按钮(对标老端建项 + SetData);null/空=清空。</summary>
        public void RefreshSkills(IList<SkillItemData> skills)
        {
            int count = skills?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                MainUISkillItem item = GetOrCreateItem(i);
                if (item == null) continue;
                item.gameObject.SetActive(true);
                item.SetData(skills[i]);
            }
            for (int i = count; i < _items.Count; i++)
            {
                if (_items[i] != null) _items[i].gameObject.SetActive(false);
            }

            if (count == 0)
            {
                GameLog.Info("MainUI", "技能列表为空 → 待对接技能数据(SkillManager)");
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
    }
}
