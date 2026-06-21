using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Skill;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 技能按钮项(对标老客户端 MainUISkillItem.ts):图标 + 锁定态 + 点击释放技能。
    ///
    /// 接真实 <see cref="SkillVo"/>(id/level/图标来自 21002 + config_skill):
    ///   · 图标:show_icon = level==0 ? GetIcon(1) : GetIcon()(对标 UpdateItem),走 ResManager.SetImageAsync + GameResPath.GetSkillIcon。
    ///   · 锁态:level==0 → 显示 lock 遮罩、点击不发事件(对标 UpdateLockState)。
    ///   · 点击:自动战斗中 → 提示不发;否则发 EVT_SKILL_SHORTCUT_CLICK(skillId, ONLY_FIRE_ATTACK)(对标 InitEvent onBtnClickHandler)。
    /// 降级(本轮记录,不造假):CirCleCdView 圆形 CD 无真实 CD 数据 → 不显示;提示文案用 GameLog(无 toast 系统),下一轮接。
    /// 克隆件不经 Show→OnInit 不自动跑,Bind 字段序列化即就绪 → 点击绑定在 SetData 内幂等兜底。
    /// </summary>
    public sealed class MainUISkillItem : MainUISkillItemBind
    {
        private SkillVo _vo;
        private bool _clickBound;

        /// <summary>由父 MainUISkillView 克隆后调用,填真实技能数据(对标 SetData → UpdateItem)。</summary>
        public void SetData(SkillVo vo)
        {
            _vo = vo;
            EnsureClickBound();

            if (vo == null)
            {
                if (icon != null) icon.enabled = false;
                UpdateLock(false);
                return;
            }

            if (icon != null)
            {
                icon.enabled = true;
                _ = ResManager.SetImageAsync(icon, GameResPath.GetSkillIcon(vo.DisplayIcon), nativeSize: false);
            }
            UpdateLock(vo.Locked);
        }

        /// <summary>对标 UpdateLockState:锁住显示 lock 遮罩。</summary>
        public void UpdateLock(bool locked)
        {
            if (@lock != null) @lock.gameObject.SetActive(locked);
        }

        /// <summary>对标 MainUISkillView.UpdateView 的 item.SetPosition(fixedPos);Laya 坐标→uGUI anchoredPosition=(x,-y)。</summary>
        public void SetPosition(float x, float y)
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null) rt.anchoredPosition = new Vector2(x, -y);
        }

        // con 是 Box 无 Graphic,点在可见的 bg 上(对标老端 con 点击;Unity 在 bg 接 raycast)。幂等绑一次。
        private void EnsureClickBound()
        {
            if (_clickBound || bg == null) return;
            bg.raycastTarget = true;
            UIUtil.AddClick(bg, OnClickSkill);
            _clickBound = true;
        }

        private void OnClickSkill()
        {
            if (_vo == null) return;

            // 锁住(未学,level==0)不可点(对标 UpdateLockState:con.mouseEnabled=!lock)。
            if (_vo.Locked)
            {
                GameLog.Info("MainUI", "技能 {0} 未解锁(level=0),点击无效", _vo.Id);
                return;
            }

            // 自动战斗中拦截(对标 GetMainRoleDepositState → Message「自动战斗中」)。
            if (AutoFightModel.Instance.AutoFightState)
            {
                GameLog.Info("MainUI", "自动战斗中,手点技能 {0} 拦截(对标 Message『自动战斗中』,toast 待接)", _vo.Id);
                return;
            }

            // 非锁、非 CD(本轮无真实 CD 数据,默认可释放)→ 发 SKILL_SHORTCUT_CLICK(对标 Fire(FightEvent.SKILL_SHORTCUT_CLICK, id, ONLY_FIRE_ATTACK))。
            EventDispatcher.Emit(GlobalEvent.EVT_SKILL_SHORTCUT_CLICK, _vo.Id, SkillManager.ONLY_FIRE_ATTACK);
        }
    }
}
