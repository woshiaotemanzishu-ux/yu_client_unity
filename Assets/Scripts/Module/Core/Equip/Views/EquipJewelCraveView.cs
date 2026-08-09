using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Jewel;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 星骸凿痕/宝石雕刻子窗(对标老客户端 jewel/EquipJewelCraveView.ts,老端外包一层单页签
    /// BaseWindowComponent「EquipJewelCraveEnterView」——Unity 走 EquipFlow.OpenSub 直接叠开内容 View,
    /// 不需要那层薄壳,同 EquipStrenMasterView 等既有先例)。
    ///
    /// 左侧装备位选择条(Content11,克隆 <see cref="EquipJewelCraveSubItem"/>):对标 InitSubView,仅铺"当前已
    /// 穿戴"的装备位(<see cref="EquipAutoWear.GetWorn"/> 1..10 遍历),每行显部位名 + 雕刻等级(真实数据,15210
    /// 落库后来自 <see cref="EquipJewelModel"/>);点击切换选中装备位 → 刷新右侧详情。
    /// 右侧:当前装备图标(cur_item_group,克隆 _tpl_EquipmentItem)+ 雕刻经验(lb_exp,原始数值直显,无分母 ——
    /// config_equip_stone_refine 未移植,无法算目标经验/进度条)+ 镶嵌宝石预览(icon1..6/lock1..6,来自
    /// GoodsDynamicModel 详情异步到位后按 StoneList 真实数据切换)。
    /// 属性对比列表(Content/_tpl_EquipJewelCraveAttItem)与雕刻材料选择器(Content1/_tpl_BaseAwardItem)依赖
    /// config_equip_stone_refine / config_equip_stone_refine_goods 两张未移植的表 → 保持隐藏,不臆造。
    ///
    /// btn_crave(单次,one_key=0)/btn_allCrave(一键,one_key=1)→ 15211,门槛提示对标老端三段拦截,按数据可用性
    /// 取舍:①「穿戴装备才能进行雕刻」(真实判定,EquipAutoWear.GetWorn==null);②「X阶以上可以雕刻」依赖
    /// config_equip_stone_refine_limit(未移植)**与装备实例"阶数"字段(config_equip_attr,Unity 未加载)**,
    /// 两者皆缺 → 跳过此拦截,交服务端 15211 guard(err152_stone_refine_limit)兜底;③「材料不足」依赖材料选择器
    /// (已degrade,无法确定该部位所需材料 type_id)→ 跳过此拦截,固定发 material_type_id=0,交服务端兜底
    /// (err150_type_err/费用判断)。
    /// </summary>
    public sealed class EquipJewelCraveView : EquipJewelCraveViewBind
    {
        private const int MaxWornPos = 10;

        private GameObject _subItemTemplate;
        private readonly System.Collections.Generic.List<EquipJewelCraveSubItem> _subItems =
            new System.Collections.Generic.List<EquipJewelCraveSubItem>();

        private GameObject _equipmentItemInstance;
        private EquipmentItem _equipmentItem;

        private Image[] _jewelIcons;
        private Image[] _jewelLocks;

        private int _selectedEquipType;

        protected override void OnInit()
        {
            if (_tpl_EquipJewelCraveAttItem != null) _tpl_EquipJewelCraveAttItem.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_EquipJewelCraveSubItem != null)
            {
                _subItemTemplate = _tpl_EquipJewelCraveSubItem;
                _subItemTemplate.SetActive(false);
            }
            HideNode(crave_red);
            HideNode(arrow_red);

            _jewelIcons = new[] { icon1, icon2, icon3, icon4, icon5, icon6 };
            _jewelLocks = new[] { lock1, lock2, lock3, lock4, lock5, lock6 };
            HideJewelPreview();

            BindClick(btn_crave, () => OnClickCrave(false));
            BindClick(btn_allCrave, () => OnClickCrave(true));
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_JEWEL_UPDATE, OnJewelUpdate);
            PopulateSubItems();
            if (_selectedEquipType <= 0 || EquipAutoWear.GetWorn(_selectedEquipType) == null)
            {
                _selectedEquipType = FindDefaultEquipType();
            }
            RefreshSubItemSelection();
            RefreshDetail();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_JEWEL_UPDATE, OnJewelUpdate);
        }

        private void OnJewelUpdate() => RefreshDetail();

        // ===================== 左侧装备位选择条 =====================

        private void PopulateSubItems()
        {
            if (_subItemTemplate == null) return;

            int shown = 0;
            for (int equipType = 1; equipType <= MaxWornPos; equipType++)
            {
                if (EquipAutoWear.GetWorn(equipType) == null) continue;
                EquipJewelCraveSubItem item = GetOrCreateSubItem(shown);
                shown++;
                if (item == null) continue;
                item.gameObject.SetActive(true);
                item.Show();   // Bind 子组件须父 Show() 触发 EnsureBound(轮3 三坑规避)
                item.SetClickCallback(OnSelectEquipType);
                item.SetData(equipType);
            }
            for (int i = shown; i < _subItems.Count; i++)
                if (_subItems[i] != null) _subItems[i].gameObject.SetActive(false);
        }

        private EquipJewelCraveSubItem GetOrCreateSubItem(int index)
        {
            while (_subItems.Count <= index)
            {
                GameObject clone = Object.Instantiate(_subItemTemplate, _subItemTemplate.transform.parent);
                _subItems.Add(clone.GetComponent<EquipJewelCraveSubItem>());
            }
            return _subItems[index];
        }

        private void RefreshSubItemSelection()
        {
            foreach (EquipJewelCraveSubItem it in _subItems)
                if (it != null) it.SetSelected(it.gameObject.activeSelf && it.EquipType == _selectedEquipType);
        }

        private static int FindDefaultEquipType()
        {
            // 老端 DefaultSelect 按"雕刻等级最低且有合成红点"挑默认项(需 RedDotManager+等级比较);
            // 本轮无红点数据源 → 简化为"取第一个已穿戴装备位"(对标降级策略,非臆造)。
            for (int equipType = 1; equipType <= MaxWornPos; equipType++)
                if (EquipAutoWear.GetWorn(equipType) != null) return equipType;
            return 0;
        }

        private void OnSelectEquipType(int equipType)
        {
            if (equipType == _selectedEquipType) return;
            _selectedEquipType = equipType;
            RefreshSubItemSelection();
            RefreshDetail();
        }

        // ===================== 右侧详情 =====================

        private void RefreshDetail()
        {
            BagGoods worn = _selectedEquipType > 0 ? EquipAutoWear.GetWorn(_selectedEquipType) : null;
            if (worn == null)
            {
                if (_equipmentItemInstance != null) _equipmentItemInstance.SetActive(false);
                if (lb_exp != null) lb_exp.text = "";
                HideJewelPreview();
                return;
            }

            EnsureEquipmentItem();
            if (_equipmentItem != null)
            {
                _equipmentItemInstance.SetActive(true);
                _equipmentItem.SetData(worn.TypeId, 0);
            }

            EquipJewelModel.CraveInfo? crave = EquipJewelModel.Instance.GetCrave(_selectedEquipType);
            if (lb_exp != null)
            {
                // 无 config_equip_stone_refine(下一级所需经验)→ 只显已获得的原始经验数值,不算进度/分母。
                lb_exp.text = crave.HasValue ? ("经验 " + crave.Value.Exp) : "";
            }

            int capturedEquipType = _selectedEquipType;
            GoodsDynamicModel.Instance.RequestDetail(worn.GoodsId, vo =>
            {
                if (capturedEquipType != _selectedEquipType) return;   // 竞态:异步回来时已切换选中装备位
                UpdateJewelPreview(vo);
            });
        }

        private void EnsureEquipmentItem()
        {
            if (_equipmentItemInstance != null || _tpl_EquipmentItem == null || cur_item_group == null) return;
            _equipmentItemInstance = Object.Instantiate(_tpl_EquipmentItem, cur_item_group);
            _equipmentItem = _equipmentItemInstance.GetComponent<EquipmentItem>();
            _equipmentItem?.Show();
        }

        private void UpdateJewelPreview(GoodsDetailVo vo)
        {
            HideJewelPreview();
            if (vo?.StoneList == null) return;
            foreach (GoodsStoneSlot slot in vo.StoneList)
            {
                int idx = slot.Pos - 1;
                if (idx < 0 || idx >= _jewelIcons.Length) continue;
                Image icon = _jewelIcons[idx];
                if (icon == null) continue;
                icon.gameObject.SetActive(true);
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(slot.TypeId);
                if (basic != null) _ = LoadJewelIconAsync(icon, basic.Icon);
            }
        }

        private async System.Threading.Tasks.Task LoadJewelIconAsync(Image icon, string iconId)
        {
            string iconPath = GameResPath.GetGoodsIconPath(iconId);
            bool ok = await ResManager.SetImageAsync(icon, iconPath, false, false);
            if (icon != null) icon.enabled = ok;
        }

        private void HideJewelPreview()
        {
            if (_jewelIcons != null) foreach (Image img in _jewelIcons) HideNode(img);
            if (_jewelLocks != null) foreach (Image img in _jewelLocks) HideNode(img);
        }

        // ===================== 雕刻按钮 =====================

        private void OnClickCrave(bool oneKey)
        {
            if (_selectedEquipType <= 0 || EquipAutoWear.GetWorn(_selectedEquipType) == null)
            {
                TipsManager.Toast("穿戴装备才能进行雕刻");   // 对标老端 !now_select_type_id_ 分支
                return;
            }
            // 老端必须先从材料列表选出有效 material_type_id；当前两张雕刻材料配置尚未落地。
            // 固定发送 0 会把一个不可操作的降级页面变成真实写协议入口，因此在配置/选择链完整前明确阻断。
            TipsManager.Toast("雕刻材料配置未就绪");
            GameLog.Warn("Equip", "点击[{0}]被阻止：config_equip_stone_refine/config_equip_stone_refine_goods 缺失",
                oneKey ? "一键雕刻" : "雕刻");
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
