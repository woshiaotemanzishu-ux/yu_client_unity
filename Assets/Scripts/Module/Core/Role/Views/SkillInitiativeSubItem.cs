using System;
using Shenxiao.Generated.UI.Role;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 主动技能子项/详情(对标老客户端 role/SkillInitiativeSubItem.ts):技能详情 + 穿戴按钮(skill_wear_btn)+ 性别立绘(_img_girl/_img_boy)
    /// + 当前/下级效果滚动列表。
    ///
    /// 降级:技能数据/穿戴协议(SkillUIModel)未移植 → skill_wear_btn 点击打 TODO,动态效果列表先不填。由界面克隆。
    /// </summary>
    public sealed class SkillInitiativeSubItem : SkillInitiativeSubItemBind
    {
        protected override void OnInit()
        {
            BindBtn(skill_wear_btn, () => GameLog.Info("Role", "穿戴主动技能 → 待对接 SkillUIModel 穿戴协议"));
        }

        private void BindBtn(Image img, Action onClick)
        {
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
