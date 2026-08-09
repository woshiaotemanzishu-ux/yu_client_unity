using Shenxiao.Generated.UI.RedPacket;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.RedPacket
{
    /// <summary>
    /// 红包主界面(对标老客户端 redPacket/RedPacketMainView.ts):红包列表(_Scroller1/Content 克隆 _tpl_RedPacketMainItem)+
    /// 记录(_btn_record→RedPacketDetailView/记录页)+ 功能/发包(_btn_func)+ 说明(_btn_help)+ 关闭(_btn_close)。
    ///
    /// 当前主窗已接回老端固定页签、说明与 33901 首屏请求；动态列表项及详情/发包子窗仍由后续增量路线接管。
    /// _btn_close → Hide 关闭返回。由二级 HUD 红包按钮(MainUIRouter "redpacket")打开。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class RedPacketMainView : RedPacketMainViewBind
    {
        private bool _listening;

        protected override void OnInit()
        {
            HideTemplates();
            if (_btn_close != null)
            {
                _btn_close.raycastTarget = true;
                UIUtil.AddClick(_btn_close, Hide);
            }
            if (_btn_record != null) UIUtil.AddClick(_btn_record, () => SwitchTab(0));
            if (_btn_func != null) UIUtil.AddClick(_btn_func, () => SwitchTab(1));
            if (_btn_help != null) UIUtil.AddClick(_btn_help, () => InstructionFlow.Show(339));
        }

        protected override void OnShow(object args)
        {
            SetListening(true);
            RedPacketController.Instance.RequestList();
            SwitchTab(0);
            GameLog.Info("RedPacket", "红包界面打开 → request 33901；动态列表待增量接管");
        }

        protected override void OnHide()
        {
            SetListening(false);
        }

        protected override void OnDispose()
        {
            SetListening(false);
        }

        internal void PrepareForRelease()
        {
            SetListening(false);
        }

        private void SetListening(bool listening)
        {
            if (_listening == listening) return;
            _listening = listening;
            if (listening) EventDispatcher.On<long>(GlobalEvent.EVT_REDPACKET_UPDATE, OnRedPacketUpdate);
            else EventDispatcher.Off<long>(GlobalEvent.EVT_REDPACKET_UPDATE, OnRedPacketUpdate);
        }

        private void OnRedPacketUpdate(long id)
        {
            GameLog.Info("RedPacket", "红包数据刷新 id={0}, list={1}, records={2}；动态列表待增量接管",
                id, RedPacketModel.Instance.List.Count, RedPacketModel.Instance.Records.Count);
        }

        private void SwitchTab(int index)
        {
            bool showRecord = index == 0;
            SetTabPagesVisible(showRecord, !showRecord);
        }

        private void SetTabPagesVisible(bool showRecord, bool showFunction)
        {
            if (_Group2 != null) _Group2.gameObject.SetActive(showRecord);
            if (_Group3 != null) _Group3.gameObject.SetActive(showFunction);
        }

        private void HideTemplates()
        {
            if (_tpl_RedPacketFuncItem != null) _tpl_RedPacketFuncItem.SetActive(false);
            if (_tpl_RedPacketMainItem != null) _tpl_RedPacketMainItem.SetActive(false);
            if (_tpl_RedPacketRecordItem != null) _tpl_RedPacketRecordItem.SetActive(false);
        }

    }
}
