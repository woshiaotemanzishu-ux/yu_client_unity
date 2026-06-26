using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Login;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 踏入仙界页(对标老客户端 LoginEnterView.ts):显示 logo / 当前服务器名 / 用户协议勾选,
    /// 「踏入仙界」触发入口解析 + 连接游戏服;点服务器名/换区盒打开选服窗。
    ///
    /// Phase 2:所有结构(logo、服务器盒、协议盒、按钮)由快照烤进 prefab,这里【只绑数据】——
    /// 不再运行时 new 节点、不再摆位置(老端 LoadSuccess 里的 vn_box_/EyouThAdultItem/tt_kefu_btn
    /// 等运行时建节点逻辑全部去掉:那是平台 SDK(抖音客服按钮、越南成人提示)的真·运行时分支,
    /// 与 data-only 移植无关)。本视图无列表/Item(老端没有 LoginEnterItem),节点与 Bind 字段 1:1,
    /// 因此没有"遍历烤好的项节点绑数据"的步骤。
    /// 数据来源:服务器名/维护态取 LoginModel.SelectedServer;协议勾选态取 LoginFlow.AgreementAgreed。
    /// </summary>
    public sealed class LoginEnterView : LoginEnterViewBind
    {
        protected override void OnInit()
        {
            // 老端 InitEvent:踏入仙界 / 换区 / 协议勾选 / 协议文本跳转。
            // 平台分支按钮(notice/cs/return/16/change_account)在 Unity 流程里默认隐藏,
            // 待对应系统接入后再点亮,这里先不绑死(对标老端按 PlatformManager 条件可见)。
            UIUtil.AddClick(_img_enter, OnClickEnter);
            UIUtil.AddClick(_box_search_server, OnClickChangeServer);
            UIUtil.AddClick(_img_search_server_bg, OnClickChangeServer);
            UIUtil.AddClick(_lb_cur_server_name, OnClickChangeServer);
            UIUtil.AddClick(_lb_tips, OnClickChangeServer);
            UIUtil.AddClick(_img_agreement_check_bg, OnClickAgreement);
            UIUtil.AddClick(_img_anreement_check, OnClickAgreement);

            // logo 走平台 logo 资源(对标老端 UpdateLogo:plat_mgr.GetLogoImgPath,默认 login/texture/logo.png)。
            // 老端走 SetOutsideImageSprite 会把 /texture/ 改写为 /other/,资源实际在 login/other/logo.png,
            // 这里直接用 other 路径取(GetIconOtherPath)。Unity 暂无 PlatformManager,用默认 logo 占位;
            // nativeSize:false 保留烤好的尺寸,不被 SetNativeSize 撑回原图大小。
            if (_img_logo != null)
                _ = ResManager.SetImageAsync(_img_logo,
                    GameResPath.GetIconOtherPath("login", "logo"), nativeSize: false);
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess 把这些通知/客服/返回按钮按平台条件显隐;Unity 流程里统一先隐藏,
            // 等通知/客服系统接入后再单独点亮。这里只处理已落地的数据。
            HidePlatformButtons();
            RefreshServer();
            RefreshAgreement();
        }

        protected override void OnHide()
        {
        }

        protected override void OnDispose()
        {
        }

        // ---------------------------------------------------------------- 服务器名(对标 UpdateServerName)

        /// <summary>当前服务器名;服务器维护中(closed==1)显示"维护中"(对标老端 UpdateServerName)。</summary>
        public void RefreshServer()
        {
            LoginServerInfo server = LoginModel.Instance.SelectedServer;
            if (server != null && server.IsClosed)
            {
                if (_lb_cur_server_name != null) _lb_cur_server_name.text = "维护中";
            }
            else if (_lb_cur_server_name != null)
            {
                _lb_cur_server_name.text = server != null ? server.DisplayName : "未选服";
            }
            SetTip("(点击换区)");
        }

        public void SetTip(string text)
        {
            if (_lb_tips != null) _lb_tips.text = text;
        }

        // ---------------------------------------------------------------- 用户协议(对标 UpdateAgreementAgreeState)

        /// <summary>勾选态:对标老端 _img_anreement_check.visible = agreement_agree_state。</summary>
        public void RefreshAgreement()
        {
            // 烤制 prefab 里 _img_anreement_check 默认 m_IsActive:0,这里用 enabled 切显隐
            // (与现行 live 视图一致,不动节点 active 避免影响烤好的初态)。
            Image check = AnreementCheck();
            if (check != null) check.enabled = LoginFlow.AgreementAgreed;
        }

        private void OnClickAgreement()
        {
            LoginFlow.ToggleAgreement();
        }

        // ---------------------------------------------------------------- 踏入仙界 / 换区

        private void OnClickEnter()
        {
            // 老端:勾选拦截 + EnterFun→BeginFun(LOGIN_STATE_CHANGE → ServerWin)。
            // 协议拦截/连接编排都在 LoginFlow.EnterGameAsync 内(对齐老端 ClientConfig.show_agreement 规则)。
            _ = LoginFlow.EnterGameAsync();
        }

        private void OnClickChangeServer()
        {
            // 老端 _box_search_server:reconnect_on_game 时直接 BeginFun,否则开选服窗。
            // Unity 选服编排收口在 LoginFlow.OpenServerSelect。
            LoginFlow.OpenServerSelect();
        }

        // ---------------------------------------------------------------- 平台分支按钮(默认隐藏)

        /// <summary>
        /// 老端按 PlatformManager/ClientConfig 条件显隐的按钮(海外返回、客服、通知、提示、切号),
        /// Unity 暂无对应平台/系统,统一隐藏,避免烤好的占位图误触/挡点击。
        /// </summary>
        private void HidePlatformButtons()
        {
            SetActive(_img_return, false);
            SetActive(_img_cs, false);
            SetActive(_img_notice, false);
            SetActive(_img_change_account, false);
            SetActive(_img_16, false);
        }

        private static void SetActive(Component node, bool active)
        {
            if (node != null) node.gameObject.SetActive(active);
        }

        // ——— 字段没绑上时按名兜底(烤制 prefab 里节点名与老端一致)———
        // _img_anreement_check 在 prefab 里是 _img_agreement_check_bg 的子节点。
        private Image AnreementCheck()
        {
            if (_img_anreement_check != null) return _img_anreement_check;
            Transform t = transform.Find("_box_agreement/_img_agreement_check_bg/_img_anreement_check");
            if (t == null)
            {
                Transform bg = _img_agreement_check_bg != null ? _img_agreement_check_bg.transform : null;
                t = bg != null ? bg.Find("_img_anreement_check") : null;
            }
            return t != null ? t.GetComponent<Image>() : null;
        }
    }
}
