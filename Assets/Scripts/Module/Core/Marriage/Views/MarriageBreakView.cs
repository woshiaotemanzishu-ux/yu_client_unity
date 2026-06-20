using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 伴侣-离婚界面(对标老客户端 marriage/MarriageBreakView.ts):
    /// 二级弹窗,从婚恋主面板「姻缘」页按钮打开;展示自己/对方头像(_head_self_con/_head_other_con 克隆 CustomHeadItem)+
    /// 名称(_lb_name_self/_lb_name_other)+ 提示文案(_lb_msg/_lb_msg2)+
    /// 协议离婚(_btn_peace → REQUEST_PROTO 17234 c=1)+ 强制离婚(_btn_force → 17234 c=2)+
    /// 消耗组(_group_cost/_img_cost/_lb_cost,按 GetConstantData(12) + 对象离线 48h 判定显隐,文案在「强制离婚/免费离婚」间切)+
    /// 关闭(_btn_close,Hide 关窗返回婚恋主面板)。
    ///
    /// 降级:MarriageModel(GetConstantData/GetMateInfo/Fire REQUEST_PROTO 17234)、FriendModel(离线判定)、
    /// GoodsModel(消耗道具图/名)、RoleManager(主角 vo)、CustomHeadItem 头像项、离婚协议 17234 均未移植 →
    /// _tpl_CustomHeadItem 模板隐藏;离婚/强制离婚按钮点击打日志「待对接」;OnShow 列表空/默认降级。
    /// 二级弹窗,_btn_close 关闭返回主面板。Null-guard 每处访问。
    /// </summary>
    public sealed class MarriageBreakView : MarriageBreakViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 Local_Open/LoadSuccess/UpdateCostData → 渲染双方头像/名称 + 按消耗与离线时长刷新「强制/免费离婚」与消耗组。
            // MarriageModel/FriendModel/GoodsModel/协议 17234/CustomHeadItem 均未移植 → 列表空/默认降级。
            GameLog.Info("Marriage", "MarriageBreakView 打开 → 待对接 MarriageModel(列表空/默认降级)");
        }

        /// <summary>列表项模板(双方头像格,由 CustomHeadItem 克隆),数据未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_CustomHeadItem != null) _tpl_CustomHeadItem.SetActive(false);
        }

        private void BindButtons()
        {
            // 关闭:Hide 关闭本二级弹窗,返回婚恋主面板。
            BindClose(_btn_close);
            BindBtn(_btn_peace, "协议离婚 REQUEST_PROTO(17234,c=1)");
            BindBtn(_btn_force, "强制离婚 REQUEST_PROTO(17234,c=2)");
        }

        /// <summary>关闭按钮(Image 或含 Image 子节点的容器)→ Hide 关闭本子窗口返回主面板。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子窗待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Marriage", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
