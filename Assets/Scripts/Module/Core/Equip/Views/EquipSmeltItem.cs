using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 神兵淬炼装备格(对标老客户端 equip/EquipSmeltItem.ts):淬炼面板里的一个部位槽,
    /// 内含装备图标(award_con 克隆 EquipmentItem)、淬炼等级 level(+N)、满阶提示 tips、
    /// 选中高亮 select_img、空位锁图标 lock_icon、强化特效 group_eff、点击背景 click_bg、红点 red_dot。
    /// 点击 click_bg → 选中该部位淬炼,经 <see cref="SetSelectCallback"/> 回调父面板(对标 SELECT_SMELT_EQUIP;
    /// 无装备提示「穿戴装备后才可以神兵淬炼该部位哦」这层文案未移植,本轮直接 no-op)。
    ///
    /// 降级:EquipModel(GetSmeltInfo/equip_smelt_info/各 EquipEvent)、EquipmentItem 子件、QiangHua 特效、
    /// UIToolTipMgr 装备 tips、淬炼/精炼配置(limit_cfg/attr_cfg)均未移植 →
    /// OnInit 隐藏红点 + 选中高亮 + 满阶提示 + 强化特效;SetData 仅做最小渲染(等级文本,无配置则空)、打日志「待对接」。
    /// 列表项,由淬炼面板克隆/铺设(本轮父面板暂未铺格,故本项当前无实例被创建;点击回调已就绪待接)。
    /// </summary>
    public sealed class EquipSmeltItem : EquipSmeltItemBind
    {
        /// <summary>该格对应的装备部位(对标 SetEquipPos 的 equip_type);0 = 未指定。</summary>
        private int _equipType;
        /// <summary>选中回调(对标 SELECT_SMELT_EQUIP 事件消费方,由 <see cref="EquipSmeltView"/> 铺格时挂)。</summary>
        private System.Action<int> _onSelect;

        protected override void OnInit()
        {
            // 红点:对标 UPDATE_SMELT_ITEM_RED,数据未移植 → 隐藏。
            if (red_dot != null) red_dot.gameObject.SetActive(false);
            // 选中高亮:对标 SetSelect,默认未选 → 隐藏。
            if (select_img != null) select_img.gameObject.SetActive(false);
            // 满阶提示:对标 is_max 时才显 tips,默认 → 隐藏。
            if (tips != null) tips.gameObject.SetActive(false);
            // 强化特效:对标 QiangHua 特效,特效系统未移植 → 隐藏占位。
            if (group_eff != null) group_eff.gameObject.SetActive(false);

            // 点击背景:对标 click_bg → 选中该部位淬炼(真调用:回调通知父面板 + 本地高亮,对标 SELECT_SMELT_EQUIP)。
            BindBtn(click_bg, () =>
            {
                if (_equipType == 0) return;
                SetSelect(true);
                _onSelect?.Invoke(_equipType);
                GameLog.Info("Equip", "选中部位淬炼 equip_type={0}(SELECT_SMELT_EQUIP)", _equipType);
            });
        }

        /// <summary>设置选中回调(由 <see cref="EquipSmeltView"/> 铺格时挂,驱动 SelectEquipType)。</summary>
        public void SetSelectCallback(System.Action<int> onSelect) => _onSelect = onSelect;

        /// <summary>设置该格部位(对标 SetEquipPos)。空位时显锁图标/可点背景。</summary>
        public void SetEquipPos(int equipType)
        {
            _equipType = equipType;
            bool hasPos = equipType != 0;
            if (click_bg != null) click_bg.gameObject.SetActive(hasPos);
            if (icon_bg != null) icon_bg.gameObject.SetActive(hasPos);
            // lock_icon 对标 role_default_{equip_type} 占位图,依赖 equipCom_asset 图集(未移植)→ 暂保持原样,不设图。
        }

        /// <summary>填淬炼数据(对标 SetData)。淬炼等级/满阶依赖 EquipModel.GetSmeltInfo + limit_cfg(未移植)→ 最小渲染。</summary>
        public void SetData(object data)
        {
            // 老端:UpdateAwardItem 克隆 EquipmentItem 显装备图标 + 读 GetSmeltInfo 算 +N/满阶。数据层未移植 → 仅清等级文本。
            if (level != null) level.text = "";
            if (tips != null) tips.gameObject.SetActive(false);
            if (select_img != null) select_img.gameObject.SetActive(false);
            GameLog.Info("Equip", "神兵淬炼格 SetData → 待对接 EquipModel(GetSmeltInfo/equip_smelt_info)+ EquipmentItem 装备图标");
        }

        /// <summary>设置选中高亮(对标 SetSelect)。</summary>
        public void SetSelect(bool selected)
        {
            if (select_img != null) select_img.gameObject.SetActive(selected);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击回调。</summary>
        private void BindBtn(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
