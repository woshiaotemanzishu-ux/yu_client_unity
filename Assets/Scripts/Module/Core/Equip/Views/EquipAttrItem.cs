using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备属性对比项(对标老客户端 equip/EquipAttrItem.ts):一行属性,左名(name)+ 当前值(attr)+ 升降提示(up)。
    /// 两种模式 —— type==1 普通属性(WordManager.GetProperties 名 + cdata 当前值 + ndata-cdata 升幅);
    /// 否则 神兵淬炼(WordManager.GetEquipPos 部位名 + EquipModel.GetSmeltInfo 精炼百分比)。gp_now 仅为布局容器(HBox)。
    ///
    /// 降级:WordManager(属性/部位文案)与 EquipModel(淬炼信息 GetSmeltInfo)均未移植 →
    /// SetData 仅按老端结构落字段(名走原始 key、值/升幅直显),文案翻译与淬炼百分比待对接;无红点/模板/按钮。
    /// 列表项,由属性面板克隆铺设。
    /// </summary>
    public sealed class EquipAttrItem : EquipAttrItemBind
    {
        protected override void OnInit()
        {
            // 无红点 / 无模板 / 无按钮 —— 纯展示项,无需隐藏件。
        }

        /// <summary>
        /// 填属性行(对标 SetData(type, cdata, ndata, equip_type))。
        /// type==1:cdata=[属性key, 当前值],ndata=[属性key, 新值] → 升幅=新值-当前值;
        /// 否则:神兵淬炼模式,cdata=精炼值(/100 → %),ndata=升幅值(/100 → %)。
        /// 降级:WordManager/EquipModel 未移植 → 名直接显原始 key、淬炼百分比待对接。
        /// </summary>
        public void SetData(int type, object[] cdata, object[] ndata, int equipType)
        {
            if (type == 1)
            {
                // 普通属性对比。
                if (name != null)
                    name.text = (cdata != null && cdata.Length > 0 ? cdata[0] : "") + "：";
                if (attr != null)
                    attr.text = (cdata != null && cdata.Length > 1 ? cdata[1].ToString() : "");
                if (up != null)
                {
                    // 升幅 = 新值 - 当前值;数值文案需 WordManager,这里只在有 ndata 时占位提示「待对接」。
                    up.text = ndata != null ? "+? (待对接 属性升幅)" : "";
                }
            }
            else
            {
                // 神兵淬炼模式 —— GetEquipPos 部位名 + GetSmeltInfo 精炼百分比(均未移植)。
                if (name != null) name.text = "神兵淬炼：";
                if (attr != null) attr.text = "0% (待对接 EquipModel.GetSmeltInfo)";
                if (up != null) up.text = "";
            }

            GameLog.Info("Equip", "EquipAttrItem.SetData(type={0}, equipType={1}) → 待对接 WordManager/EquipModel 文案", type, equipType);
        }
    }
}
