using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 天殒淬炉大师/全身强化总览(对标老客户端 equip/EquipStrenMasterView.ts):标题 + 当前效果组(group_cur/Content1)+
    /// 下一阶效果组(group_next/Content)+ 等级进度(lb_stren1/2/3)+ 激活按钮(btn_active/lb_active)+ 激活红点(img_redAc)+ 关闭。
    /// 老端 open 后请求协议 15261、监听 UPDATE_MASTER_VIEW 刷新两段属性列表(EquipMasterItem/EquipNextMasterItem),
    /// 激活点 15260;等级取 EquipModel.GetAllStrenLv/GetMasterNextLv,满阶时 cur_tip="已满阶"、按钮置灰。
    ///
    /// 降级:EquipModel/协议(15260/15261)、WordManager、LoopScrowViewMgr 列表均未移植 →
    /// 激活红点(img_redAc)隐藏、属性模板(_tpl_EquipMasterItem)隐藏、列表空;激活/关闭按钮点击打日志「待对接」;
    /// OnShow 打 TODO(列表空、等级/属性默认降级)。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipStrenMasterView : EquipStrenMasterViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess 请求协议 15261,UPDATE_MASTER_VIEW 回包后铺当前/下一阶属性列表 + 刷等级进度。
            // 数据层未移植 → 列表空、属性/等级默认降级。
            GameLog.Info("Equip", "EquipStrenMasterView 打开 → 待对接 EquipModel/协议(列表空/属性默认降级)");
        }

        private void HideReds()
        {
            // img_redAc:激活红点(cur_lv >= next_lv 时亮),降级先隐藏。
            HideNode(img_redAc);
        }

        private void HideTemplates()
        {
            if (_tpl_EquipMasterItem != null) _tpl_EquipMasterItem.SetActive(false);
        }

        private void BindButtons()
        {
            BindBtn(btn_active, "激活/全身强化(协议 15260)");
            BindBtn(btn_close, "关闭");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:逻辑/协议待对接)。</summary>
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
