using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 属性行(对标老端 fashion/FashionAttrItem.ts SetData(pair, curPair, nextPair)):
    /// name=属性名、_lb_att0="+当前值"(未激活/无当前档时按 0)、_lb_att1="+下一档值" 或 "已满阶"。
    /// 名称/万分比换算复用 <see cref="GoodsModel"/>(对标老端 WordManager.GetProperties/ConvertToPercentValue)。
    /// </summary>
    public sealed class FashionAttrItem : FashionAttrItemBind
    {
        /// <summary>curVal=当前档该属性值(无当前档传0);hasNext=false 时显示"已满阶"。</summary>
        public void SetData(int attrId, long curVal, bool hasNext, long nextVal)
        {
            // 名称与当前值由 Prefab 的 __name_value_row/HorizontalLayoutGroup 按 preferred width
            // 保持 8px 间距；View 只赋数据，不再把老端运行时坐标重新写回视觉层。
            if (name != null) name.text = GoodsModel.GetAttrName(attrId);
            if (_lb_att0 != null) _lb_att0.text = "+" + GoodsModel.FormatAttrValue(attrId, curVal);
            if (_lb_att1 != null) _lb_att1.text = hasNext ? ("+" + GoodsModel.FormatAttrValue(attrId, nextVal)) : "已满阶";
        }
    }
}
