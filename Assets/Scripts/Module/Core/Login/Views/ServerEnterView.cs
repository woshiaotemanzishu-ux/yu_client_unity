using System;
using Shenxiao.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 服务器展示 / 踏入仙界页(重构版,自包含独立 prefab)——取代老端碎片化的 LoginEnterView。
    ///
    /// 结构(对标截图3 + 截图2):全屏立绘背景 + logo「九州神霄录」 + 当前服按钮(状态图标 + 服名 + 提示) +
    /// 底部「踏入仙界」按钮 + 协议勾选行(勾选指示 + 文案);用户协议弹层(默认隐:协议正文 + 拒绝 + 同意)同 prefab 内 SetActive 切换。
    /// ServerEnterView.prefab 是唯一视觉事实源；布局、图片、字号和间距直接在 Unity Prefab 中编辑。
    ///
    /// 本类只做:① 数据绑定 ② 功能性状态切换(协议勾选指示显隐 / 弹层显隐)。不写颜色/字号/尺寸等视觉样式。
    /// 逻辑从老 LoginEnterView.cs 原样搬过来(协议/选服/进服全经 LoginFlow 静态方法)。
    ///
    /// ── 对接说明(供主控接 LoginFlow)──────────────────────────────────────────
    /// 暴露给 LoginFlow 回调的 public 方法:
    ///   void RefreshServer()          当前服名 ← LoginModel.Instance.SelectedServer?.DisplayName(无则「未选服」)
    ///   void RefreshAgreement()       按 LoginFlow.AgreementAgreed 更新勾选指示(勾图 SetActive)
    ///   void ShowAgreementAlert()     显示协议弹层
    ///   void HideAgreementAlert()     隐藏协议弹层
    /// 协议流程(对标老客户端「不同意不得进入」):
    ///   进入本页 OnShow → 未同意(LoginFlow.AgreementAgreed==false)自动弹协议弹层;
    ///   点勾选行 / 协议文案 → 也弹协议弹层(勾选态不在此直接翻转);
    ///   弹层「同意」→ LoginFlow.SetAgreement(true)(勾选 + 按账号记录);
    ///   弹层「不同意」→ LoginFlow.SetAgreement(false)(不勾选);
    ///   踏入仙界 → LoginFlow.EnterGameAsync():未勾选则被拦截并重弹协议层。
    /// 点击动作调用的 LoginFlow 静态方法:
    ///   当前服按钮(图标/服名/提示)→ LoginFlow.OpenServerSelect()
    ///   踏入仙界按钮             → LoginFlow.EnterGameAsync()
    /// </summary>
    [UIView("prefabs/ui/login/serverenterview")]
    public sealed class ServerEnterView : BaseView
    {
        [Header("当前服务器")]
        public Image serverBtn;                 // 当前服按钮底图(命中体)
        public Image serverStateIcon;           // 服务器状态图标
        public TextMeshProUGUI serverNameLabel; // 当前服名
        public TextMeshProUGUI tipLabel;        // 固定操作提示(点击换区)

        [Header("踏入仙界")]
        public Image enterBtn;                   // 踏入仙界按钮(命中体)
        public TextMeshProUGUI enterBtnLabel;

        [Header("运营公告")]
        public Image noticeBtn;
        public TextMeshProUGUI noticeBtnLabel;

        [Header("用户协议勾选")]
        public Image agreementCheckBg;           // 勾选框底图(命中体)
        public Image agreementCheckMark;         // 勾选标记(按勾选态 SetActive)
        public TextMeshProUGUI agreementLabel;   // 「我已仔细阅读并同意 用户协议」文案(命中体)

        [Header("用户协议弹层(默认隐)")]
        public GameObject agreementAlert;        // 弹层根
        public TextMeshProUGUI agreementContent; // 协议正文
        public TmpLinkClickHandler agreementLinkHandler; // 正文内《用户协议》《隐私保护指引》链接
        public Image agreementCancelBtn;         // 拒绝(命中体)
        public Image agreementOkBtn;             // 同意(命中体)

        protected override void OnShow(object args)
        {
            HideAgreementAlert();
            BindClicks();
            RefreshServer();
            RefreshAgreement();
            // 进入本页:未同意协议则自动弹协议弹层(同意后才可踏入仙界)。
            if (!LoginFlow.AgreementAgreed) ShowAgreementAlert();
        }

        protected override void OnHide()
        {
            agreementLinkHandler?.ClearHandler();
        }

        private void BindClicks()
        {
            // 当前服按钮:底图 / 服名 / 提示 任意一处都能换区(对标老 LoginEnterView)
            ClearAndAddClick(serverBtn, OnClickChangeServer);
            ClearAndAddClick(serverNameLabel, OnClickChangeServer);
            ClearAndAddClick(tipLabel, OnClickChangeServer);

            ClearAndAddClick(enterBtn, OnClickEnter);
            ClearAndAddClick(enterBtnLabel, OnClickEnter);
            ClearAndAddClick(noticeBtn, LoginFlow.OpenLoginNotice);
            ClearAndAddClick(noticeBtnLabel, LoginFlow.OpenLoginNotice);

            // 协议勾选:框 / 文案 任意一处切换
            ClearAndAddClick(agreementCheckBg, OnClickAgreement);
            ClearAndAddClick(agreementCheckMark, OnClickAgreement);
            ClearAndAddClick(agreementLabel, OnClickAgreement);

            // 弹层按钮:拒绝/同意(转交注入的回调)
            ClearAndAddClick(agreementCancelBtn, OnClickAlertCancel);
            ClearAndAddClick(agreementOkBtn, OnClickAlertOk);
            agreementLinkHandler?.SetHandler(OnClickAgreementLink);
        }

        // ---------------------------------------------------------------- 数据绑定 / 功能性状态切换

        /// <summary>当前服名 ← LoginModel.Instance.SelectedServer?.DisplayName。</summary>
        public void RefreshServer()
        {
            LoginServerInfo server = LoginModel.Instance.SelectedServer;
            if (serverNameLabel != null) serverNameLabel.text = server != null ? server.DisplayName : "未选服";
            if (tipLabel != null) tipLabel.text = "(点击换区)";
        }

        /// <summary>按 LoginFlow.AgreementAgreed 更新勾选指示(勾图显隐,功能性指示)。</summary>
        public void RefreshAgreement()
        {
            if (agreementCheckMark != null) agreementCheckMark.gameObject.SetActive(LoginFlow.AgreementAgreed);
        }

        // ---------------------------------------------------------------- 协议弹层

        /// <summary>显示协议弹层(默认隐→显);同意/不同意由弹层按钮回落到 LoginFlow.SetAgreement。</summary>
        public void ShowAgreementAlert()
        {
            if (agreementAlert != null) agreementAlert.SetActive(true);
        }

        /// <summary>隐藏协议弹层。</summary>
        public void HideAgreementAlert()
        {
            if (agreementAlert != null) agreementAlert.SetActive(false);
        }

        // ---------------------------------------------------------------- 事件(逻辑从老 LoginEnterView 搬)

        private void OnClickEnter()
        {
            _ = LoginFlow.EnterGameAsync();
        }

        private void OnClickChangeServer()
        {
            LoginFlow.OpenServerSelect();
        }

        private void OnClickAgreement()
        {
            // 点勾选行/协议文案 → 弹出协议弹层;勾选态只由弹层的 同意/不同意 决定(不在此直接翻转)。
            ShowAgreementAlert();
        }

        private void OnClickAlertOk()
        {
            HideAgreementAlert();
            LoginFlow.SetAgreement(true);    // 同意:勾选 + 按账号记录
        }

        private void OnClickAlertCancel()
        {
            HideAgreementAlert();
            LoginFlow.SetAgreement(false);   // 不同意:不勾选(踏入仙界会被拦截)
        }

        private static void OnClickAgreementLink(string linkId)
        {
            switch (linkId)
            {
                case "agreement":
                    LoginFlow.OpenAgreementDocument(LoginUserAgreementView.TypeAgreement);
                    break;
                case "privacy":
                    LoginFlow.OpenAgreementDocument(LoginUserAgreementView.TypePrivacy);
                    break;
            }
        }

        private static void ClearAndAddClick(Graphic target, Action onClick)
        {
            if (target == null) return;
            UIUtil.ClearClicks(target);   // 每次 OnShow 重绑前先清,防监听叠加
            UIUtil.AddClick(target, onClick);
        }
    }
}
