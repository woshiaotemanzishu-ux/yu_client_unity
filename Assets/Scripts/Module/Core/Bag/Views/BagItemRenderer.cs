using Shenxiao.Generated.UI.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包格子项(对标老客户端 bag/BagItemRenderer.ts):一个背包槽,内含克隆的 <see cref="BaseAwardItem"/>(真实物品图标 +
    /// 品质底板 + 数量),外加品质 grade/星级 star/锁 lock/限时 time_limit/装备升降箭头 up·down/禁用 ban/劣质 _bad_icon/红点 等覆盖件。
    ///
    /// 第 8 轮 P1:内联模板 _tpl_BaseAwardItem 是 common/BaseAwardItem.prefab 的嵌套实例(第 6 轮已回填 BaseAwardItem View 组件),
    /// 故克隆即得可用 View → SetData 走真实图标(对标 BaseAwardItem.SetData → GoodsModel.config_goods 图标/品质底板)。
    /// 列表项常被 <see cref="Views.BagComponentView"/> 克隆后直接 SetData(不经 BaseView.Show),OnInit 不会自动跑 →
    /// 用 <see cref="EnsureInit"/> 幂等保证模板克隆 + 覆盖件隐藏就位(不依赖 Show 时序)。
    /// 装备态覆盖件由共享格自身按 config_equip_attr + BagGoods 实例态恢复；列表页不再把这些状态一刀切隐藏。
    /// </summary>
    public sealed class BagItemRenderer : BagItemRendererBind
    {
        private BaseAwardItem _item;
        private bool _inited;

        protected override void OnInit()
        {
            EnsureInit();
        }

        /// <summary>幂等初始化:克隆 BaseAwardItem 模板进 conta(对标 load_callback:new BaseAwardItem(this.conta))+ 隐覆盖件。</summary>
        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            if (_tpl_BaseAwardItem != null && conta != null)
            {
                GameObject go = Instantiate(_tpl_BaseAwardItem, conta);
                go.SetActive(true);
                _item = go.GetComponent<BaseAwardItem>();
            }
            ResetOverlays();
        }

        /// <summary>填格子数据(对标 dataChanged);null/Count&lt;=0 = 空槽。有物品则走 BaseAwardItem 真实图标 + 品质底板 + 数量。</summary>
        public void SetData(BagItemData data)
        {
            EnsureInit();
            bool hasItem = data != null && data.Count > 0;
            ShowEmpty(!hasItem);
            ResetOverlays();

            // 真实图标 + 品质底板 + 数量(对标 BaseAwardItem.SetData → GoodsModel.GetGoodsBasicByTypeId → goods_icon/color)。
            if (hasItem && _item != null)
            {
                bool bound = data.Goods != null && data.Goods.Bind != 0;
                _item.SetData(data.TypeId, data.Count, bound);
                // 点击带 BagGoods 实例 → 装备实例 tips(极品 equip_extra_attr / 强化 stren);无实例(完成弹层等)走 BaseAwardItem 默认 Show(typeId,num)。
                if (data.Goods != null) _item.SetClickCallBack(() => ItemTipsView.Show(data.Goods));
                else _item.SetClickCallBack(null);
            }

            if (hasItem) RefreshPresentation(data);
        }

        private void ShowEmpty(bool empty)
        {
            if (defaultImg != null) defaultImg.gameObject.SetActive(empty);
            if (_item != null) _item.gameObject.SetActive(!empty);
        }

        private void ResetOverlays()
        {
            SetActive(up, false);
            SetActive(down, false);
            SetActive(ban, false);
            SetActive(grade, false);
            if (grade != null) grade.text = "";
            SetActive(star_group, false);
            SetActive(star_1, false);
            SetActive(star_2, false);
            SetActive(star_3, false);
            SetActive(star_4, false);
            SetActive(@lock, false);
            SetActive(time_limit, false);
            SetActive(redMask, false);
            SetActive(_bad_icon, false);
            SetActive(group_eff, false);
        }

        private void RefreshPresentation(BagItemData data)
        {
            BagGoods goods = data?.Goods;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(data?.TypeId ?? 0);
            if (basic == null) return;

            // BaseAwardItem 负责配置型限时；背包外层补实例 expire_time，与老端 BagItemRenderer 一致。
            SetActive(time_limit, goods != null && goods.ExpireTime > 0);
            if (basic.Type != 10) return;

            GoodsModel.EquipAttr equip = GoodsModel.GetEquipAttr(data.TypeId);
            int stage = equip?.Stage ?? 0;
            int star = Mathf.Clamp(equip?.Star ?? 0, 0, 4);
            if (grade != null)
            {
                grade.text = stage > 0 ? stage + "阶" : "";
                grade.gameObject.SetActive(stage > 0);
            }
            SetActive(star_group, star > 0);
            SetActive(star_1, star >= 1);
            SetActive(star_2, star >= 2);
            SetActive(star_3, star >= 3);
            SetActive(star_4, star >= 4);
            SetActive(_bad_icon, equip?.ClassType == 1);

            bool blocked = !CanWear(basic);
            SetActive(ban, blocked);
            if (blocked || goods == null) return;

            BagGoods worn = basic.EquipType > 0 ? BagModel.Instance.GetEquipmentAt(basic.EquipType) : null;
            SetActive(up, worn == null || worn.Rating < goods.Rating);
            SetActive(down, worn != null && worn.Rating > goods.Rating);
        }

        private static bool CanWear(GoodsModel.GoodsBasic basic)
        {
            RoleModel role = RoleModel.Instance;
            if (role.Level < basic.Level) return false;
            if (basic.CareerId != 0 && basic.CareerId != role.Career) return false;
            if (basic.Sex != 0 && basic.Sex != role.Sex) return false;
            return basic.Turn <= (role.Figure?.turn ?? 0);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }
    }

    /// <summary>背包格子数据(对标 BagGoods 的显示字段:type_id → 真实图标/品质,goods_num → 数量)。
    /// <see cref="Goods"/> 携真实 <see cref="BagGoods"/> 实例(装备 tips 极品/强化实例属性透传;非背包来源可空 → 走 typeId 默认 tips)。</summary>
    public sealed class BagItemData
    {
        public int TypeId;
        public long Count;
        public BagGoods Goods;
    }
}
