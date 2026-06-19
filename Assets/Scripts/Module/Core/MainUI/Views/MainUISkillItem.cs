using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 技能按钮项(对标老客户端 MainUISkillItem.ts):图标 + 锁定态 + CD 遮罩,点击释放技能。
    ///
    /// 降级:SkillManager / skill_vo / FightEvent.SKILL_SHORTCUT_CLICK / CirCleCdView 圆形 CD 遮罩
    /// 均未移植 → 用最小 DTO <see cref="SkillItemData"/> 填图标/锁定;点击打日志「待对接技能释放」;
    /// CD 遮罩先不显示(待 CirCleCdView + 技能 CD 数据移植)。
    /// </summary>
    public sealed class MainUISkillItem : MainUISkillItemBind
    {
        private SkillItemData _data;
        private bool _clickBound;

        /// <summary>由父 MainUISkillView 克隆后调用,填技能数据。</summary>
        public void SetData(SkillItemData data)
        {
            _data = data;
            if (data == null) return;

            if (icon != null && !string.IsNullOrEmpty(data.Icon))
                _ = ResManager.SetImageAsync(icon, GameResPath.GetSkillIcon(data.Icon), nativeSize: false);
            UpdateLock(data.Locked);

            // 对标 InitEvent:点击容器释放技能。con 是 Box 无 Graphic,点在可见的 bg 上。
            if (!_clickBound && bg != null)
            {
                bg.raycastTarget = true;
                UIUtil.AddClick(bg, OnClickSkill);
                _clickBound = true;
            }
        }

        /// <summary>对标 UpdateLockState:锁住显示 lock 遮罩。</summary>
        public void UpdateLock(bool locked)
        {
            if (@lock != null) @lock.gameObject.SetActive(locked);
        }

        private void OnClickSkill()
        {
            if (_data == null) return;
            // TODO 待接:自动战斗中拦截;CD 中提示「技能冷却」;否则 Fire(SKILL_SHORTCUT_CLICK, skill_id)。
            GameLog.Info("MainUI", "点击技能 id={0} → 待对接技能释放 SKILL_SHORTCUT_CLICK", _data.SkillId);
        }
    }

    /// <summary>技能项最小数据(待 SkillManager/skill_vo 移植后填充)。</summary>
    public sealed class SkillItemData
    {
        public int SkillId;
        public string Icon;
        public bool Locked;
    }
}
