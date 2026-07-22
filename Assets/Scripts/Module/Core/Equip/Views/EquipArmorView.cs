using Shenxiao.Generated.UI.EquipArmor;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 不朽圣骸页(对标老客户端 equipArmor/EquipArmorView.ts,EquipView 标签5 内容):圣骸部位页签(_gp_tabs 克隆 _tpl_ArmorTabItem)+
    /// 属性(_gp_attr/_gp_attr2)+ 材料(_gp_mat)+ 当前/对比(_gp_curr/gp_msg)+ 制作按钮(_btn_make)+ 全部(_btn_all)+ 左右翻页(leftBtn/rightBtn)+ 红点。
    ///
    /// 降级:14401 权威快照已落 ArmorModel，但本 View 尚未接配置/列表消费与14402打造 → 3 红点/3模板隐藏、列表空;
    /// 制作/全部/翻页按钮仍打日志降级。
    /// 无独立关闭按钮(装备窗框统一关闭)→ 作 EquipView 标签5 内容由 EquipFlow 跨模块 reparent 进窗框内容区。事件驱动,不进 FirstPass。
    /// </summary>
    public sealed class EquipArmorView : EquipArmorViewBind
    {
        protected override void OnInit()
        {
            HideNode(_img_red);
            HideNode(_img_red1);
            HideNode(_img_red2);
            if (_tpl_ArmorItem != null) _tpl_ArmorItem.SetActive(false);
            if (_tpl_ArmorTabItem != null) _tpl_ArmorTabItem.SetActive(false);
            if (_tpl_ArmorAttrItem != null) _tpl_ArmorAttrItem.SetActive(false);
            BindBtn(_btn_make, "不朽圣骸·制作");
            BindBtn(img_make, "不朽圣骸·制作");
            BindBtn(_btn_all, "不朽圣骸·全部");
            BindBtn(_btn_left, "不朽圣骸·上一页");
            BindBtn(_btn_right, "不朽圣骸·下一页");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 ArmorModel + 配置铺部位/属性/材料；当前只有14401数据地基，仍默认降级。
            GameLog.Info("Equip", "不朽圣骸页打开 → 14401已接，待对接配置/UI消费与14402(默认降级)");
        }

        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Equip", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
