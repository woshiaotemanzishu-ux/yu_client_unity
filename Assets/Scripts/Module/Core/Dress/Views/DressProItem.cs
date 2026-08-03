using Shenxiao.Generated.UI.Dress;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Dress
{
    public sealed class DressProItem : DressProItemBind
    {
        public void SetData(int attrId, long current, long next)
        {
            string name = GoodsModel.GetAttrName(attrId);
            if (string.IsNullOrEmpty(name)) name = "属性" + attrId;
            if (now_label != null) now_label.text = name + " " + GoodsModel.FormatAttrValue(attrId, current);
            if (next_label != null) next_label.text = "+" + GoodsModel.FormatAttrValue(attrId, next);
            if (next_arrow != null) next_arrow.gameObject.SetActive(next > current || (current == 0 && next > 0));
        }
    }
}
