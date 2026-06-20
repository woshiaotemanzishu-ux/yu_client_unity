using Shenxiao.Generated.UI.RedPacket;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.RedPacket
{
    /// <summary>
    /// 红包主界面(对标老客户端 redPacket/RedPacketMainView.ts):红包列表(_Scroller1/Content 克隆 _tpl_RedPacketMainItem)+
    /// 记录(_btn_record→RedPacketDetailView/记录页)+ 功能/发包(_btn_func)+ 说明(_btn_help)+ 关闭(_btn_close)。
    ///
    /// 降级:RedPacketModel/红包协议、RedPacketMainItem/RecordItem/FuncItem 列表项均未移植 → 列表空、_tpl_* 模板隐藏;
    /// _btn_record/_btn_func/_btn_help 打日志降级;_btn_close → Hide 关闭返回。由二级 HUD 红包按钮(MainUIRouter "redpacket")打开。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class RedPacketMainView : RedPacketMainViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();
            if (_btn_close != null)
            {
                _btn_close.raycastTarget = true;
                UIUtil.AddClick(_btn_close, Hide);
            }
            BindBtn(_btn_record, "红包记录");
            BindBtn(_btn_func, "发红包/功能");
            BindBtn(_btn_help, "说明");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 请求红包列表 + 铺 RedPacketMainItem。RedPacketModel/协议未移植 → 列表空降级。
            GameLog.Info("RedPacket", "红包界面打开 → 待对接 RedPacketModel(列表空降级)");
        }

        private void HideTemplates()
        {
            if (_tpl_RedPacketFuncItem != null) _tpl_RedPacketFuncItem.SetActive(false);
            if (_tpl_RedPacketMainItem != null) _tpl_RedPacketMainItem.SetActive(false);
            if (_tpl_RedPacketRecordItem != null) _tpl_RedPacketRecordItem.SetActive(false);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子窗待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("RedPacket", "点击[{0}] → 待对接", label));
        }
    }
}
