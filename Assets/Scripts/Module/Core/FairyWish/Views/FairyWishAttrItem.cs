using Shenxiao.Generated.UI.FairyWish;

namespace Shenxiao.Module.Core.FairyWish
{
    /// <summary>
    /// 仙缘许愿属性项(对标老客户端 fairyWish/FairyWishAttrItem.ts):属性名(_lb_attr_name)+ 值(_lb_attr_value)。
    /// SetData(name, value)。由 FairyWishView 克隆。
    /// </summary>
    public sealed class FairyWishAttrItem : FairyWishAttrItemBind
    {
        public void SetData(string attrName, string attrValue)
        {
            if (_lb_attr_name != null) _lb_attr_name.text = attrName ?? "";
            if (_lb_attr_value != null) _lb_attr_value.text = attrValue ?? "";
        }
    }
}
