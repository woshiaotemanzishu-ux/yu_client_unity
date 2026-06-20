using Shenxiao.Generated.UI.Role;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 被动技能子项(对标老客户端 role/SkillPassiveSubItem.ts):技能名(_lb_name)+ 当前(_lb_now)/下级(_lb_next)效果描述 + 开启条件(lb_open)。
    ///
    /// SetData(name)。降级:升级数据/效果文本(SkillManager)未移植 → 名即时,其余待接。由 SkillPassiveItem 克隆。
    /// </summary>
    public sealed class SkillPassiveSubItem : SkillPassiveSubItemBind
    {
        public void SetData(string name)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
        }
    }
}
