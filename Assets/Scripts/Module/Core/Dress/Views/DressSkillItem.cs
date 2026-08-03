using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Dress;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Dress
{
    /// <summary>技能模板保留业务子类；当前只读 11200 切片不虚构未下发技能状态。</summary>
    public sealed class DressSkillItem : DressSkillItemBind
    {
        private int _skillId;
        private bool _visualReady;

        public int SkillId => _skillId;
        public bool IsVisualReady => _visualReady;

        protected override void OnInit()
        {
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            if (skill_img != null) UIUtil.AddClick(skill_img, () => DressSkillTipFlow.Show(_skillId));
        }

        public async void SetData(int skillId, int unlockLevel, int currentLevel)
        {
            _skillId = skillId;
            _visualReady = false;
            bool locked = currentLevel < unlockLevel;
            if (condition != null) condition.text = locked ? unlockLevel + "级激活" : "";
            if (condition_bg != null) condition_bg.gameObject.SetActive(locked);
            if (skill_img == null) return;
            skill_img.color = locked ? new Color32(125, 125, 125, 255) : Color.white;
            bool loaded = await ResManager.SetImageAsync(
                skill_img, GameResPath.GetSkillIconPath(skillId.ToString()), nativeSize: false);
            if (this == null || _skillId != skillId) return;
            skill_img.gameObject.SetActive(loaded);
            _visualReady = loaded;
        }
    }
}
