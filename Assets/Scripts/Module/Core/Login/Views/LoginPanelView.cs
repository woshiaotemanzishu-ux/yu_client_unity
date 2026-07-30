using System;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录页(重构版,自包含独立 prefab)——取代老端碎片化的 LoginView + RegisterView。
    ///
    /// 结构(对标用户给的截图):一张卡片承载表单,「账号/密码」标签在输入框左侧,记住密码居中,
    /// 底部两按钮并排(登录页:注册 | 登录;注册页:确定注册 | 返回),两子面板同 prefab 内 SetActive 切换。
    /// 当前 Prefab 是唯一视觉事实源，背景、布局及下列 public 引用均直接序列化保存。
    ///
    /// 本类只做:① 数据绑定 ② 功能性状态切换(切子面板 / 记住勾选指示)。不写颜色/字号/尺寸等样式。
    /// 接入流程:LoginFlow 注入 LoginSubmit/RegisterSubmit,走完整登录链路(校验→选服→EnterLobby);
    /// 未注入时走独立兜底，仅供隔离运行时预览自测。
    /// </summary>
    [UIView("prefabs/ui/login/loginpanel")]
    public sealed class LoginPanelView : BaseView
    {
        [Header("子面板")]
        public GameObject loginGroup;
        public GameObject registerGroup;

        [Header("登录子面板")]
        public TMP_InputField loginAccount;
        public TMP_InputField loginPassword;
        public Image loginBtn;
        public TextMeshProUGUI loginBtnLabel;
        public Image gotoRegisterBtn;
        public Image checkImg;    // 记住密码·未选中状态图(CheckImg);仅切显隐,不改 sprite
        public Image checkImg1;   // 记住密码·选中状态图(CheckImg1)
        public TextMeshProUGUI rememberLabel;

        [Header("注册子面板")]
        public TMP_InputField registerAccount;
        public TMP_InputField registerPassword;
        public Image confirmBtn;
        public Image returnBtn;

        /// <summary>接入流程时由 LoginFlow 注入:走完整链路。为空则走独立兜底(自测预览)。</summary>
        public Func<string, string, bool, Task> LoginSubmit;
        public Func<string, string, Task> RegisterSubmit;

        private bool _remember = true;
        private bool _busy;

        protected override void OnShow(object args)
        {
            ShowLogin();
        }

        // ---------------------------------------------------------------- 功能性状态切换(允许)

        /// <summary>切到登录子面板(隐藏注册)。</summary>
        public void ShowLogin()
        {
            if (registerGroup != null) registerGroup.SetActive(false);
            if (loginGroup != null) loginGroup.SetActive(true);
            BindLoginClicks();
            RestoreSavedInput();
        }

        /// <summary>切到注册子面板(隐藏登录)。</summary>
        public void ShowRegister()
        {
            if (loginGroup != null) loginGroup.SetActive(false);
            if (registerGroup != null) registerGroup.SetActive(true);
            BindRegisterClicks();
            // 开发期默认值:仅当为空时填随机账号/密码(不覆盖已输入内容)。
            if (registerAccount != null && string.IsNullOrEmpty(registerAccount.text)) registerAccount.text = RandomDigits();
            if (registerPassword != null && string.IsNullOrEmpty(registerPassword.text)) registerPassword.text = RandomDigits();
        }

        /// <summary>登录中态:由 LoginFlow 在提交期间调用(改按钮文案 + 防重入)。</summary>
        public void SetBusy(bool busy)
        {
            _busy = busy;
            if (loginBtnLabel != null) loginBtnLabel.text = busy ? "登录中" : "登录";
        }

        // ---------------------------------------------------------------- 数据绑定

        private void RestoreSavedInput()
        {
            SavedLoginInput saved = LoginController.Instance.LoadSavedInput();
            if (loginAccount != null && string.IsNullOrEmpty(loginAccount.text)) loginAccount.text = saved.account;
            _remember = saved.remember;
            if (_remember && loginPassword != null && string.IsNullOrEmpty(loginPassword.text)) loginPassword.text = saved.password;
            RefreshRemember();
        }

        private void BindLoginClicks()
        {
            ClearAndAddClick(loginBtn, OnClickLogin);
            ClearAndAddClick(loginBtnLabel, OnClickLogin);
            ClearAndAddClick(gotoRegisterBtn, ShowRegister);
            ClearAndAddClick(checkImg, OnClickRemember);
            ClearAndAddClick(checkImg1, OnClickRemember);
            ClearAndAddClick(rememberLabel, OnClickRemember);
        }

        private void BindRegisterClicks()
        {
            ClearAndAddClick(confirmBtn, OnClickConfirm);
            ClearAndAddClick(returnBtn, ShowLogin);
        }

        private static void ClearAndAddClick(Graphic target, Action onClick)
        {
            if (target == null) return;
            UIUtil.ClearClicks(target);   // 每次切面板重绑前先清,防监听叠加
            UIUtil.AddClick(target, onClick);
        }

        // ---------------------------------------------------------------- 事件

        private async void OnClickLogin()
        {
            if (_busy) return;
            string acc = loginAccount != null ? loginAccount.text : string.Empty;
            string pwd = loginPassword != null ? loginPassword.text : string.Empty;

            if (LoginSubmit != null)
            {
                await LoginSubmit(acc, pwd, _remember);   // 接入流程:LoginFlow 收口(SetBusy/提示/EnterLobby)
                return;
            }

            // 独立预览兜底
            _busy = true;
            try
            {
                LoginRequestResult r = await LoginController.Instance.LoginAsync(acc, pwd, _remember);
                if (!r.success) TipsManager.Toast(r.message);
                else GameLog.Info("Login", "登录成功(LoginPanelView 独立预览)");
            }
            finally { _busy = false; }
        }

        private async void OnClickConfirm()
        {
            if (_busy) return;
            string acc = (registerAccount != null ? registerAccount.text : string.Empty).Trim();
            string pwd = (registerPassword != null ? registerPassword.text : string.Empty).Trim();
            if (string.IsNullOrEmpty(acc) || string.IsNullOrEmpty(pwd))
            {
                TipsManager.Toast("请输入账号密码");   // 对标老端 RegisterView:51(此校验在正式流程也走)
                return;
            }

            if (RegisterSubmit != null)
            {
                await RegisterSubmit(acc, pwd);
                return;
            }

            _busy = true;
            try
            {
                LoginRequestResult r = await LoginController.Instance.RegisterAsync(acc, pwd, true);
                if (!r.success) TipsManager.Toast(r.message);
                else GameLog.Info("Login", "注册并登录成功(LoginPanelView 独立预览)");
            }
            finally { _busy = false; }
        }

        private void OnClickRemember()
        {
            _remember = !_remember;
            RefreshRemember();
        }

        /// <summary>记住密码指示:按记住态切两张状态图的显隐(功能性指示,不碰 sprite/样式)。</summary>
        private void RefreshRemember()
        {
            if (checkImg1 != null) checkImg1.gameObject.SetActive(_remember);    // 选中态图
            if (checkImg != null) checkImg.gameObject.SetActive(!_remember);     // 未选中态图
        }

        private static string RandomDigits()
        {
            return UnityEngine.Random.Range(0, 100000).ToString();
        }
    }
}
