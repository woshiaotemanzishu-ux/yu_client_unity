using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临装备格(对标老客户端 godBefall/GodBefallEquipItem.ts):一个神契装备槽。
    /// 老端三态 —— status1 未解锁(_img_bg=ui_js_pzk_01、_img_icon=gb_pos_{pos}、藏神图标/锁2/星);
    /// status2 空位(同上,点击请求穿戴 44012 或打开 GodBefallWayView);
    /// status3 已穿装备(按品质 Color2BgRes 换底、克隆 EquipmentItem 显图标、按 star 显 _img_star_1..4、
    /// 绑定锁图 _img_lock2、神等级图标 _img_god_icon、点击弹装备 Tips)。红点 _img_red 走 GOD_EQUIP/GOD_QUICK_COM。
    ///
    /// 降级:GodBefallModel(GetCurCanSetEquip2/GetEquipInfo/GetEquipData/GetRedStatus)、EquipModel/GoodsModel、
    /// EquipmentItem 子件、ResManager 图标加载、UIToolTipMgr 装备 Tips、星级/品质配置、ShowTween1 动画均未移植 →
    /// OnInit 隐红点 _img_red + 模板 _tpl_EquipmentItem + 神图标/锁2/星容器,_box_con 点击仅打日志;
    /// SetData 仅保留数据引用并打日志「待对接」,不做图标/品质/星级渲染。列表项,由神祇降临面板克隆铺设。
    /// </summary>
    public sealed class GodBefallEquipItem : GodBefallEquipItemBind
    {
        /// <summary>该格绑定的原始数据(对标老端 this.data,含 pos/god_id/goods_id/unlock/empty);降级仅持有引用。</summary>
        private object _data;

        protected override void OnInit()
        {
            // 红点:对标 _img_red(GOD_EQUIP / GOD_QUICK_COM 红点),数据未移植 → 隐藏。
            HideNode(_img_red);
            // 模板:EquipmentItem 克隆模板(GameObject)→ 直接 SetActive(false)(HideNode 收 Component)。
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            // 神等级图标 / 绑定锁 / 星级容器:对标 status3 才显,默认 → 隐藏(SetData 接入后再按数据开)。
            HideNode(_img_god_icon);
            HideNode(_img_lock2);
            HideNode(_box_star);

            // 点击背景:对标 _box_con 点击(status1 提示 / status2 请求穿戴 44012 / status3 弹装备 Tips)→ 降级仅打日志。
            BindBtn(_box_con, "神祇降临装备格点击 → 待对接 GodBefallModel(穿戴 44012 / 装备 Tips)");
        }

        /// <summary>
        /// 填装备格数据(对标老端 SetData(data) → UpdateItem)。
        /// 老端按 data.unlock / data.empty / data.goods_id 分三态渲染底图/图标/星级/品质;
        /// 降级:GodBefallModel / EquipModel / GoodsModel / ResManager / EquipmentItem 均未移植 →
        /// 仅保留数据引用 + 打日志「待对接」,不做任何图标/品质/星级渲染。
        /// </summary>
        public void SetData(object data)
        {
            _data = data;
            GameLog.Info("GodBefall", "GodBefallEquipItem.SetData → 待对接 GodBefallModel/EquipModel/GoodsModel(三态:未解锁/空位/已穿装备 的底图/图标/星级/品质)");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:逻辑/协议待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("GodBefall", "点击[{0}] → 待对接", label));
        }

        /// <summary>安全隐藏一个节点(对标老端 visible=false / 模板隐藏)。</summary>
        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        /// <summary>安全隐藏一个 GameObject 模板(对标 _tpl_* 占位)。</summary>
        private static void HideNode(GameObject go)
        {
            if (go != null) go.SetActive(false);
        }
    }
}
