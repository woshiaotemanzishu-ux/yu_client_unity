using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.InnateSkill;
using Shenxiao.Module.Core.Skill;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 天赋技能树单个技能图标(对标老客户端 innateSkill/InnateSkillItem.ts):图标(lv==0 置灰)+ 等级 + 选中态。
    /// 点击选中 → 通知宿主 <see cref="InnateSkillView"/> 刷新信息区/升级区(老端 Fire SELECT_INNATE_ITEM 全局事件,
    /// 本端改走直接回调,避免为一次性选中态新增全局事件)。
    ///
    /// 由 <see cref="InnateListItem"/> 从隐藏模板(_gp_item 下唯一子节点)按需 Instantiate 克隆、复用池管理;
    /// 点击只在 OnInit 绑定一次(对标 SkillPassiveSubItem.EnsureClickBound 惯例),闭包读 <see cref="SkillId"/> 字段
    /// 避免克隆复用后新增重复监听。
    /// </summary>
    public sealed class InnateSkillItem : InnateSkillItemBind
    {
        public int SkillId { get; private set; }

        /// <summary>点击回调(由 <see cref="InnateListItem"/> 在放置该 item 时赋值/更新)。</summary>
        public System.Action<int> OnClicked;

        private bool _clickBound;

        protected override void OnInit()
        {
            if (_clickBound || _group == null) return;
            UIUtil.AddClick(_group, () => { if (SkillId > 0) OnClicked?.Invoke(SkillId); });
            _clickBound = true;
        }

        public void SetData(int skillId, int skillType, bool selected)
        {
            SkillId = skillId;
            if (skillId <= 0 || !SkillConfigs.Has(skillId))
            {
                gameObject.SetActive(false);
                return;
            }

            int lv = SkillTalentModel.Instance.GetTalentLevel(skillId);
            int maxLv = SkillConfigs.GetMaxLevel(skillId);
            bool locked = lv <= 0;

            if (_img_icon != null)
            {
                _img_icon.gameObject.SetActive(true);
                _img_icon.color = locked ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
                _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetSkillIcon(SkillConfigs.GetIconForLevel(skillId, Mathf.Max(lv, 1))), nativeSize: false);
            }
            if (_Image1 != null) _Image1.color = locked ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
            if (_Image2 != null) _Image2.color = locked ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
            if (_lb_lv != null) _lb_lv.text = lv + "/" + maxLv;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }
    }
}
