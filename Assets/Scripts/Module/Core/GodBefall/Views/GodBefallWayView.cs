using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临-玩法说明弹窗(对标老客户端 godBefall/GodBefallWayView.ts):标题(lb_title)+ 说明文案(_Label1/_Label2)+
    /// 关闭按钮(_btn_close)+ 前往按钮(_btn_get);其余 _ImageN 为底图/装饰。活动层居中弹窗,点背景关闭。
    ///
    /// 老端行为:_btn_close → Close();_btn_get → Close() + 关闭 GodBefallEquipView(CLOSE_VIEW 事件) + OpenFun.OpenFunHandler(135) 跳转。
    /// 降级:GlobalEventSystem/OpenFun(功能跳转 135)未移植 → 按钮点击仅打日志「待对接」;无红点、无模板;文案/底图走预制体默认。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class GodBefallWayView : GodBefallWayViewBind
    {
        protected override void OnInit()
        {
            // 无红点字段、无 _tpl_* 模板 —— 仅绑定按钮(关闭 / 前往)。
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 load_callback/InitEvent:仅绑事件,文案由布局静态承载。数据未移植 → 走默认。
            GameLog.Info("GodBefall", "GodBefallWayView 打开 → 待对接 GodBefallModel/协议(列表空/默认降级)");
        }

        private void BindButtons()
        {
            BindBtn(_btn_close, "关闭");
            BindBtn(_btn_get, "前往(关闭 GodBefallEquipView + OpenFun 135)");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/跳转待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("GodBefall", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
