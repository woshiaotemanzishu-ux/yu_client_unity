using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋主界面(对标老客户端 marriage/MarriageBaseView.ts):分页容器,左侧页签(_group_btn 克隆 MarriageBaseBtn)
    /// 切 大厅(MarriageFriendView)/姻缘(MarriageMainView)/指环(MarriageRingView)/锦囊(MarriageGiftView)/副本(MarriageDunView)
    /// 五页(box_con/_viewStack 容纳页内容);右上关闭按钮 _btn_close;老端 click_bg_toClose 点背景关。
    ///
    /// 降级:MarriageModel/RoleManager/RedDotManager 与 5 个子页(MarriageFriend/Main/Ring/Gift/DunView)均未移植 →
    /// 页签与页内容空;_tpl_* 模板先隐藏;_btn_close 关闭可用(直接 Hide 自身,Flow 的 Toggle 据 IsShown 复开);OnShow 打日志。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。其余 39 个婚恋子页待后续 tick 逐批接入。
    /// </summary>
    public sealed class MarriageBaseView : MarriageBaseViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();

            // 关闭按钮:老端 BaseWindowComponent 关窗;此处直接 Hide 自身(模块内窗口由 MarriageFlow 持有,Toggle 据 IsShown 复开)。
            if (_btn_close != null)
            {
                _btn_close.raycastTarget = true;
                UIUtil.AddClick(_btn_close, Hide);
            }
        }

        protected override void OnShow(object args)
        {
            // 老端 open_callback → OpenView:按会籍/等级铺页签 + 打开默认页(大厅)。数据/子页未移植 → 空降级。
            GameLog.Info("Marriage", "婚恋主界面打开 → 待对接 MarriageModel(页签/大厅/姻缘/指环/锦囊/副本 子页空降级)");
        }

        /// <summary>页签按钮与 5 个子页模板(由 MarriageFlow/各子页 View 克隆铺设),数据未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_MarriageBaseBtn != null) _tpl_MarriageBaseBtn.SetActive(false);
            if (_tpl_MarriageDunView != null) _tpl_MarriageDunView.SetActive(false);
            if (_tpl_MarriageFriendView != null) _tpl_MarriageFriendView.SetActive(false);
            if (_tpl_MarriageGiftView != null) _tpl_MarriageGiftView.SetActive(false);
            if (_tpl_MarriageMainView != null) _tpl_MarriageMainView.SetActive(false);
            if (_tpl_MarriageRingView != null) _tpl_MarriageRingView.SetActive(false);
        }
    }
}
