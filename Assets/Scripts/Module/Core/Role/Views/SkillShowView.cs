using Shenxiao.Generated.UI.Role;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 技能解锁展示弹窗(对标老客户端 role/SkillShowView.ts):技能图标(_img_icon)+ 名(_lb_name)+ 说明(_lb_desc),自动上浮淡出。
    ///
    /// SetData(name, desc)。降级:技能图标(配置)+ 补间淡出动画(Laya.Tween)未移植 → 文本即时、图标/动画待接。
    /// 事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class SkillShowView : SkillShowViewBind
    {
        public void SetData(string name, string desc)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
            if (_lb_desc != null) _lb_desc.text = desc ?? "";
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Role", "技能解锁展示 → 待对接 技能配置(SkillManager);补间淡出动画待接");
        }
    }
}
