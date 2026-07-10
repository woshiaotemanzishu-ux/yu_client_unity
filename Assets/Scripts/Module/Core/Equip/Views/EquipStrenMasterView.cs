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
    /// 全身奖励协议(15260/15261,经 EquipStrenController,自动循环 轮4 队列#4)已接线:OnShow → QueryWholeAward()
    /// (对标老端 LoadSuccess 发 15261);btn_active → ActivateWhole(type=1)(老端会先本地比较 cur_lv/next_lv 拦截,
    /// 该比较依赖 EquipModel.GetAllStrenLv/GetMasterNextLv 未移植 → 本轮不拦截,直接发,由服务端 err152_lv_limit
    /// 兜底);btn_close → 真关闭(Hide())。
    /// 降级:EquipModel 两段属性列表(EquipMasterItem/EquipNextMasterItem)、WordManager 均未移植 →
    /// 激活红点(img_redAc)隐藏、属性模板(_tpl_EquipMasterItem)隐藏、列表空、等级进度文本默认降级。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
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
            // 属性列表渲染未移植 → 列表空、等级进度默认降级;协议查询已接真。
            GameLog.Info("Equip", "EquipStrenMasterView 打开 → 请求 15261(列表渲染/等级进度仍默认降级)");
            EquipStrenController.Instance.QueryWholeAward();
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
            BindClick(btn_active, () =>
            {
                GameLog.Info("Equip", "点击[激活] → ActivateWhole(type=1)");
                EquipStrenController.Instance.ActivateWhole(1);
            });
            BindClick(btn_close, () =>
            {
                GameLog.Info("Equip", "点击[关闭] → Hide()");
                Hide();
            });
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击回调。</summary>
        private void BindClick(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
