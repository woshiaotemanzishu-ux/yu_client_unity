using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Baby
{
    public sealed class BabyPropItem : BabyPropItemBind
    {
        public void SetData(int attrId, long currentValue, bool hasNext, long nextValue)
        {
            if (nameLb != null)
                nameLb.text = GoodsModel.GetAttrName(attrId) + " <color=#ff8a00>" + GoodsModel.FormatAttrValue(attrId, currentValue) + "</color>";
            if (arrow != null) arrow.gameObject.SetActive(hasNext);
            if (nextLb != null)
                nextLb.text = hasNext ? "+" + GoodsModel.FormatAttrValue(attrId, nextValue - currentValue) : string.Empty;
        }
    }
}
