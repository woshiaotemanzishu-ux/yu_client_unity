using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备熔炉属性行项(对标老客户端 equip/EquipMasterItem.ts):一行展示一条属性 —— 名(lb_name)+ 数值(lb_attr)。
    /// 老端 dataChanged 用 data 数组渲染:data[0] 有属性ID → lb_name = WordManager.GetProperties(id)、lb_attr = "+"+data[1];
    /// data[0] 空 → lb_name = "全身天殒淬炉"、lb_attr = "+"+(data[1]/100)+"%"。
    ///
    /// 降级:属性名映射(WordManager.GetProperties)/熔炉数据源未移植 → SetData 仅做纯展示(直接填名+数值字符串),
    /// 不查配置;空数据走老端「全身」兜底文案。本项无红点/模板。列表项,由熔炉列表克隆。
    /// </summary>
    public sealed class EquipMasterItem : EquipMasterItemBind
    {
        protected override void OnInit()
        {
            // 本项无红点、无 _tpl_* 模板,OnInit 无需隐藏;属性色对标老端 lb_attr.color = "#4a3a32"。
            if (lb_attr != null) lb_attr.color = new Color32(0x4a, 0x3a, 0x32, 0xff);
        }

        /// <summary>填属性行(对标 dataChanged/UpdateView):name 为属性名(空则走「全身」兜底),attr 为已格式化的数值文本。</summary>
        public void SetData(string name, string attr)
        {
            if (lb_name != null) lb_name.text = string.IsNullOrEmpty(name) ? "全身天殒淬炉" : name;
            if (lb_attr != null) lb_attr.text = attr ?? "";
        }

        /// <summary>降级展示(无数据源时):直接显「全身」兜底,等熔炉数据(EquipModel/WordManager)移植后由列表调 SetData。</summary>
        public void ShowEmpty()
        {
            if (lb_name != null) lb_name.text = "全身天殒淬炉";
            if (lb_attr != null) lb_attr.text = "";
            GameLog.Info("Equip", "装备熔炉属性行 → 待对接 EquipModel/WordManager(属性名+数值)");
        }
    }
}
