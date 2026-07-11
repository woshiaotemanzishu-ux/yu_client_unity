using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Jewel;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 宝石镶嵌槽(对标老客户端 jewel/EquipJewelItem.ts):装备当前选中件的一个镶嵌位 —— 图标(icon)+ 未镶嵌提示
    /// (_add)+ 未解锁锁图(@lock)+ 名称/门槛文案(tips)+ 合成红点(red_dot/arrowImg)+ 升级特效层(_gp_eff)。
    /// 点击 _gp_click → 打开宝石背包弹窗(对标老端 Fire(OPEN_VIEW,"EquipJewelBagView",...),经
    /// <see cref="EquipFlow"/>.OpenSub 带参重载传递 equip_pos/stone_pos/当前 type_id 上下文)。
    ///
    /// 列表项,由 <see cref="EquipJewelView"/>.CreateJewelItems 克隆铺入 pos_1..pos_6(对标老端 6 个镶嵌位,
    /// <c>EquipDefine.JEWEL_POS=[1..6]</c>)。**当前烤图(JewelModule.prefab)未提供 EquipJewelItem 的克隆模板**
    /// (pos_1..pos_6 是空容器,无 `_tpl_EquipJewelItem` 可查),故本项当前无实例被创建 —— 与 EquipWashItem/
    /// EquipWashPropItem 同款先例("列表项已就绪待接,父面板暂未铺格")。
    ///
    /// 降级:未解锁门槛(config_equip_stone_pos_unlock 未移植)→ 默认按"已解锁但未镶嵌"展示(不臆造锁定,
    /// 交打开背包弹窗后由服务端 15208 guard 兜底);红点(合成/雕刻)未接数据源 → 隐藏。
    /// </summary>
    public sealed class EquipJewelItem : EquipJewelItemBind
    {
        private int _equipPos;
        private int _stonePos;
        private int _currentTypeId;

        protected override void OnInit()
        {
            HideNode(red_dot);
            HideNode(arrowImg);
            HideNode(_gp_eff);

            BindClick(_gp_click, () =>
            {
                GameLog.Info("Equip", "点击镶嵌槽 equip_pos={0} stone_pos={1} type_id={2} → 打开 EquipJewelBagView",
                    _equipPos, _stonePos, _currentTypeId);
                EquipFlow.OpenSub("EquipJewelBagView",
                    new EquipJewelBagView.Context(_equipPos, _stonePos, _currentTypeId));
            });
        }

        /// <summary>设置该槽位序号(对标 SetJewelPos,1..6)。</summary>
        public void SetJewelPos(int pos) => _stonePos = pos;

        /// <summary>设置该槽位所属装备位(对标 SetInfos 存 dynamic.equip_type,供打开背包弹窗时带上下文)。</summary>
        public void SetEquipPos(int equipPos) => _equipPos = equipPos;

        /// <summary>已镶嵌(对标 SetData):显图标 + 名称,隐藏未镶嵌/锁图标。</summary>
        public void SetData(int typeId)
        {
            _currentTypeId = typeId;
            if (@lock != null) @lock.gameObject.SetActive(false);
            if (_add != null) _add.gameObject.SetActive(false);
            if (icon != null) icon.enabled = false;

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (tips != null) tips.text = basic != null ? basic.Name : "";
            if (basic != null && icon != null) _ = LoadIconAsync(typeId, basic.Icon);
        }

        /// <summary>空槽(对标 SetLockEmptyStatus 简化版,门槛表未移植 → 一律按"已解锁未镶嵌"展示):
        /// 显未镶嵌加号,隐图标/锁。</summary>
        public void SetEmpty()
        {
            _currentTypeId = 0;
            if (@lock != null) @lock.gameObject.SetActive(false);
            if (_add != null) _add.gameObject.SetActive(true);
            if (icon != null) icon.enabled = false;
            if (tips != null) tips.text = "未镶嵌";
        }

        private async System.Threading.Tasks.Task LoadIconAsync(int typeId, string iconId)
        {
            string iconPath = GameResPath.GetGoodsIconPath(iconId);
            bool ok = await ResManager.SetImageAsync(icon, iconPath, false, false);
            if (_currentTypeId != typeId) return;   // 竞态:异步返回时槽位数据已变
            icon.enabled = ok;
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        private void BindClick(Component target, System.Action onClick)
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
