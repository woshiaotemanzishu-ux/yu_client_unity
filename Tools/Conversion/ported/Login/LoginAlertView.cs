using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Login;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 用户协议提示弹窗(对标 Laya LoginAlertView.ts)。
    ///
    /// data-only:背景/标题/拒绝盒/同意盒/关闭按钮/富文本说明 这些结构都已烤进 prefab,
    /// 这里【只绑数据 + 接点击】——不运行时 new 节点、不摆位置(老端 GetComponents 里
    /// 对 _html_content 的 width/style/lineHeight 等纯布局赋值已删,布局由 prefab 承担)。
    ///
    /// 老端语义:
    ///   _box_ok    → Close + 触发 AGREE_LOGIN_ALERT(同意协议、进入游戏);
    ///   _box_cancel→ Close(+ QQ/MiFast 小游戏退出小程序,Unity 客户端不适用,见 risks);
    ///   _img_close → Close;
    ///   _html_content 的 <a> 富文本链接点击 → 打开 LoginUserAgreementView(见 risks,Unity 暂无对应视图)。
    ///
    /// 同意/拒绝的真正业务回调由 LoginFlow 通过 ShowWith 注入(对标老端 AGREE_LOGIN_ALERT 事件),
    /// 与现有 LoginFlow.ShowAgreement() 的调用约定保持一致(drop-in)。
    /// </summary>
    public sealed class LoginAlertView : LoginAlertViewBind
    {
        private Action _onOk;
        private Action _onCancel;

        protected override void OnInit()
        {
            // 一次性:无。点击监听放到 OnShow 重绑(先 ClearClicks 防叠加),对标老端 InitEvent 在 LoadSuccess 内执行。
        }

        protected override void OnShow(object args)
        {
            // 老端 InitEvent:每次展示前重绑点击,先清后加,避免重复 Show 叠加监听。
            // 老端把点击绑在 _box_ok / _box_cancel 整盒上;但 prefab 里这两个盒的 Image 是 disabled+无 sprite,
            // 且 Bind 字段为 0(fileID 0)未绑。已绑且 enabled 的命中体是盒内的按钮图 _img_ok/_img_cancel 与
            // 文字 _lb_ok/_lb_cancel——绑这些更稳妥(等价整组按钮可点)。关闭走 _img_close。
            BindClick(_img_ok, OnClickOk);
            BindClick(_lb_ok, OnClickOk);
            BindClick(_img_cancel, OnClickCancel);
            BindClick(_lb_cancel, OnClickCancel);
            BindClick(_img_close, OnClickCancel);

            // 富文本说明已烤进 prefab 的 _html_content(老端两分支文案一致),这里不在运行时覆盖文本。
            // 老端 _html_content 的 LINK 事件用于打开协议详情页;Unity 暂无 LoginUserAgreementView 对应实现,
            // 见 risks,这里不接,避免 new 不存在的视图。
        }

        protected override void OnHide()
        {
        }

        protected override void OnDispose()
        {
            _onOk = null;
            _onCancel = null;
        }

        /// <summary>对标老端:由 LoginFlow 注入同意/拒绝回调后展示(与现有 LoginFlow.ShowAgreement 约定一致)。</summary>
        public void ShowWith(Action onOk, Action onCancel)
        {
            _onOk = onOk;
            _onCancel = onCancel;
            Show();
        }

        private void OnClickOk()
        {
            // 老端:Close + Fire(AGREE_LOGIN_ALERT)。AGREE_LOGIN_ALERT 的后续(勾选/记账号/进游戏)
            // 由 LoginFlow 注入的 _onOk 承接。
            Hide();
            _onOk?.Invoke();
        }

        private void OnClickCancel()
        {
            // 老端 cancel 分支里 QQ/MiFast 小游戏会 exitMiniProgram();Unity 客户端无此平台分支(见 risks)。
            Hide();
            _onCancel?.Invoke();
        }

        // ——— 点击重绑:先 ClearClicks 再 AddClick,防 OnShow 重复绑定叠加(对标样板 BindBakedCareers)———
        private static void BindClick(Graphic target, Action onClick)
        {
            if (target == null) return;
            target.raycastTarget = true;
            UIUtil.ClearClicks(target);
            UIUtil.AddClick(target, onClick);
        }
    }
}
