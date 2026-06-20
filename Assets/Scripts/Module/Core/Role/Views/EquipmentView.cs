using Shenxiao.Generated.UI.Role;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 角色装备/属性主界面(对标老客户端 role/EquipmentView.ts):角色模型(main_role/model_gp)+ 名(_lb_name)/等级(top_levelLb)+
    /// 战力(_gp_fight)+ 装备格(icon_gp)+ 二级菜单(secondary_menu)+ 技能红点(skill_red)等。角色页核心。
    ///
    /// 降级:RoleManager/EquipModel/场景模型未移植 → 技能红点隐藏、名/等级默认、OnShow 打 TODO;角色 3D 模型由场景系统驱动(待接)。
    /// 事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipmentView : EquipmentViewBind
    {
        protected override void OnInit()
        {
            if (skill_red != null) skill_red.gameObject.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Role", "角色装备界面打开 → 待对接 角色数据(RoleManager/EquipModel)+ 场景模型");
        }
    }
}
