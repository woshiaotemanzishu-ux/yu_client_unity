using Shenxiao.Generated.UI.Equip;
using Shenxiao.Module.Core.Common;
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
                int attrId = ReadInt(cdata, 0);
                long current = ReadLong(cdata, 1);
                long next = ReadLong(ndata, 1, current);
                SetStrengthPreview(attrId, current, next, ndata != null);
            }
            else
            {
                // 神兵淬炼模式 —— GetEquipPos 部位名 + GetSmeltInfo 精炼百分比(均未移植)。
                if (name != null) name.text = "神兵淬炼：";
                if (attr != null) attr.text = "0% (待对接 EquipModel.GetSmeltInfo)";
                if (up != null) up.text = "";
            }

        }

        /// <summary>强化属性预览：真实属性名、当前值和下一级增量。</summary>
        public void SetStrengthPreview(int attrId, long current, long next, bool hasNext)
        {
            string attrName = GoodsModel.GetAttrName(attrId);
            if (string.IsNullOrEmpty(attrName)) attrName = "属性" + attrId;
            if (name != null) name.text = attrName + "：";
            if (attr != null) attr.text = GoodsModel.FormatAttrValue(attrId, current);
            if (up != null)
            {
                long delta = next - current;
                up.text = hasNext && delta > 0
                    ? "+" + GoodsModel.FormatAttrValue(attrId, delta)
                    : string.Empty;
            }
        }

        public void SetSmeltPreview(int equipType, int currentRatio, int nextRatio)
        {
            string posName = GoodsModel.GetEquipPosName(equipType);
            if (name != null) name.text = (string.IsNullOrEmpty(posName) ? "装备" : posName) + "神兵淬炼：";
            if (attr != null) attr.text = (currentRatio / 100d).ToString("0.##") + "%";
            if (up != null) up.text = nextRatio > 0 ? ("+" + (nextRatio / 100d).ToString("0.##") + "%") : string.Empty;
        }

        private static int ReadInt(object[] values, int index, int fallback = 0)
        {
            if (values == null || index < 0 || index >= values.Length || values[index] == null) return fallback;
            return System.Convert.ToInt32(values[index]);
        }

        private static long ReadLong(object[] values, int index, long fallback = 0)
        {
            if (values == null || index < 0 || index >= values.Length || values[index] == null) return fallback;
            return System.Convert.ToInt64(values[index]);
        }
    }
}
