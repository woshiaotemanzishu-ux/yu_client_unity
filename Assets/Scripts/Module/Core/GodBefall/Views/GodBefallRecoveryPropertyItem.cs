using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临-恢复属性对比项(对标老客户端 godBefall/GodBefallRecoveryPropertyItem.ts):
    /// 一行属性 —— 属性名(lable_property_cur_name)+ 当前值(lable_property_cur_value)
    /// + 箭头(img_arrorw)+ 下一阶值(lable_property_next_value)。
    /// SetData(curr_attr=[属性key, 当前值], next_attr=[属性key, 新值])→ 名走 WordManager.GetProperties 翻译。
    ///
    /// 降级:WordManager(属性 key → 中文文案)未移植 → 名直接显原始 key;
    /// 无红点 / 无模板 / 无按钮 —— 纯展示项,由 GodBefallRecoveryTopView 克隆铺设。
    /// </summary>
    public sealed class GodBefallRecoveryPropertyItem : GodBefallRecoveryPropertyItemBind
    {
        protected override void OnInit()
        {
            // 无红点 / 无模板 / 无按钮 —— 纯展示项,无需隐藏件。
        }

        /// <summary>
        /// 填属性行(对标 SetData(data) → UpdatePropertyInfo)。
        /// currAttr=[属性key, 当前值];nextAttr=[属性key, 新值],为 null 时不显下一阶值。
        /// 降级:WordManager.GetProperties 未移植 → 名直接显原始 key,文案翻译待对接。
        /// </summary>
        public void SetData(object[] currAttr, object[] nextAttr)
        {
            // 当前属性名:老端走 WordManager.GetProperties(curr_attr[0]),未移植 → 直显原始 key。
            string currName = (currAttr != null && currAttr.Length > 0 && currAttr[0] != null)
                ? currAttr[0].ToString() : "";
            if (lable_property_cur_name != null)
                lable_property_cur_name.text = currName + "：";

            // 当前值:curr_attr[1]。
            if (lable_property_cur_value != null)
                lable_property_cur_value.text = (currAttr != null && currAttr.Length > 1 && currAttr[1] != null)
                    ? currAttr[1].ToString() : "";

            // 下一阶值:next_attr ? next_attr[1] : ""。
            string nextValue = (nextAttr != null && nextAttr.Length > 1 && nextAttr[1] != null)
                ? nextAttr[1].ToString() : "";
            if (lable_property_next_value != null)
                lable_property_next_value.text = nextValue;

            // 箭头:无下一阶值时无意义 → 跟随 nextValue 显隐。
            if (img_arrorw != null)
                img_arrorw.gameObject.SetActive(!string.IsNullOrEmpty(nextValue));

            GameLog.Info("GodBefall", "GodBefallRecoveryPropertyItem.SetData → 待对接 WordManager.GetProperties 属性文案翻译");
        }
    }
}
