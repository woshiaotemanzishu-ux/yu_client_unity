using Shenxiao.Generated.UI.Role;
using UnityEngine.UI;

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
            // Prefab 用 ContentSizeFitter + HorizontalLayoutGroup 对标老端 HBox。文本改变后立即完成两级布局，
            // 避免同一帧截图/切页仍沿用模板“攻击:”的旧宽度，长属性名覆盖数值。
            if (property_name != null) LayoutRebuilder.ForceRebuildLayoutImmediate(property_name.rectTransform);
            if (property_group != null) LayoutRebuilder.ForceRebuildLayoutImmediate(property_group);
        }
    }
}
