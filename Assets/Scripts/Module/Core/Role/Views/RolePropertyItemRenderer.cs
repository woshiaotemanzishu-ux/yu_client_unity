using Shenxiao.Generated.UI.Role;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 角色属性项(对标老客户端 role/RolePropertyItemRenderer.ts):属性名(property_name)+ 值(property_value)。
    /// SetData(attr, value)。由角色属性界面克隆。
    /// </summary>
    public sealed class RolePropertyItemRenderer : RolePropertyItemRendererBind
    {
        public void SetData(string attr, string value)
        {
            if (property_name != null) property_name.text = (attr ?? "") + ":";
            if (property_value != null) property_value.text = value ?? "";
        }
    }
}
