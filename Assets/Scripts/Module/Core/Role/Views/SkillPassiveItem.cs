using System;
using System.Collections.Generic;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Role;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.Skill;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 被动技能滚动列表项，对标老客户端 role/SkillPassiveItem.ts。
    /// 只负责图标、名称、等级、选中态、可升级红点和点击回调。
    /// </summary>
    public sealed class SkillPassiveItem : SkillPassiveItemBind
    {
        private SkillVo _skill;
        private Action<SkillVo> _onSelected;
        private bool _clickBound;

        protected override void OnInit()
        {
            if (_img_select != null) _img_select.gameObject.SetActive(false);
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            EnsureClickBound();
        }

        public void BindData(SkillVo vo, bool selected, Action<SkillVo> onSelected)
        {
            EnsureClickBound();
            _skill = vo;
            _onSelected = onSelected;

            if (_lb_name != null) _lb_name.text = vo?.GetName() ?? string.Empty;
            if (_lb_lv != null) _lb_lv.text = vo != null ? vo.Level.ToString() : string.Empty;
            if (_img_select != null) _img_select.gameObject.SetActive(selected && vo != null);

            if (_img_icon != null)
            {
                _img_icon.enabled = vo != null;
                if (vo != null)
                {
                    _ = ResManager.SetImageAsync(
                        _img_icon,
                        GameResPath.GetSkillIcon(vo.DisplayIcon),
                        nativeSize: false);
                }
            }

            int typeId = 0;
            int need = 0;
            bool canUpgrade = vo != null && vo.TryGetNextLevelGoodsCost(out typeId, out need);
            bool enough = canUpgrade && BagModel.Instance.GetTypeGoodsNum(typeId) >= need;
            if (_img_red != null) _img_red.gameObject.SetActive(enough);
        }

        public static List<SkillVo> GetOrderedPassiveSkills()
        {
            var result = new List<SkillVo>();
            List<SkillPassiveConfigs.PassiveSkillCfg> configured =
                SkillPassiveConfigs.GetForCareer(RoleModel.Instance.Career);
            foreach (SkillPassiveConfigs.PassiveSkillCfg cfg in configured)
            {
                if (cfg == null || cfg.SkillId <= 0) continue;
                SkillVo vo = SkillManager.Instance.GetSkill(cfg.SkillId) ?? new SkillVo(cfg.SkillId);
                vo.Level = DungeonModel.Instance.GetHeartSkillLevel((uint)cfg.SkillId);
                vo.TaskId = cfg.TaskId;
                result.Add(vo);
            }
            return result;
        }

        private void EnsureClickBound()
        {
            if (_clickBound || _img_2 == null) return;
            _img_2.raycastTarget = true;
            UIUtil.ClearClicks(_img_2);
            UIUtil.AddClick(_img_2, () => _onSelected?.Invoke(_skill));
            _clickBound = true;
        }
    }
}
