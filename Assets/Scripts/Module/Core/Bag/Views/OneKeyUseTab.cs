using Shenxiao.Generated.UI.Bag;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 一键使用页签项(对标老客户端 bag/OneKeyUseTab.ts):分类页签,显示名字 + 选中态(Select/Unselect 互显)。
    ///
    /// SetData(name) 填名字、SetSelect(bool) 切选中。降级:页签数据由 OneKeyUseView 提供(未移植时不克隆);
    /// 选中切换是自包含视觉逻辑。列表项,由 OneKeyUseView 克隆 _tpl_OneKeyUseTab。
    /// </summary>
    public sealed class OneKeyUseTab : OneKeyUseTabBind
    {
        /// <summary>填页签名字。</summary>
        public void SetData(string name)
        {
            if (nameLb != null) nameLb.text = name ?? "";
        }

        /// <summary>选中切换(Select/Unselect 互显)。</summary>
        public void SetSelect(bool selected)
        {
            if (Select != null) Select.gameObject.SetActive(selected);
            if (Unselect != null) Unselect.gameObject.SetActive(!selected);
        }
    }
}
