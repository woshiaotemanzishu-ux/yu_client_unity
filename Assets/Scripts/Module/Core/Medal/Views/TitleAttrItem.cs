using Shenxiao.Generated.UI.Title;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Medal
{
    /// <summary>天境当前/下一星属性对比行。</summary>
    public sealed class TitleAttrItem : TitleAttrItemBind
    {
        public void SetData(int attrId, long current, long next, bool showNext)
        {
            string name = GoodsModel.GetAttrName(attrId);
            if (string.IsNullOrEmpty(name)) name = "属性" + attrId;
            if (now_attr_lb != null)
                now_attr_lb.text = name + "：<color=#c85a37>" + GoodsModel.FormatAttrValue(attrId, current) + "</color>";

            bool visible = showNext && next != 0;
            if (next_gp != null) next_gp.gameObject.SetActive(visible);
            if (next_attr_lb != null)
            {
                bool percent = GoodsModel.FormatAttrValue(attrId, next).Contains("%");
                string color = percent ? "#fe1a1a" : "#0a953e";
                next_attr_lb.text = "<color=" + color + ">" + GoodsModel.FormatAttrValue(attrId, next) + "</color>";
            }
        }
    }
}
