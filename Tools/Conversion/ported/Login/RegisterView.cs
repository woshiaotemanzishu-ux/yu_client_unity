using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 注册页(对标老客户端 login/RegisterView.ts)。
    ///
    /// Phase 2:结构(背景/账号·密码标签组/确定·返回按钮/logo)由快照烤进 prefab,这里【只绑数据】——
    /// 不在运行时 new 节点、不摆位置。老 TS 的 InitEvent/UpdateView 语义:
    ///   confirmBtn:校验账号密码非空 → 注册并自动登录(老端 Fire COMFIRM_REGISTER_BTN);空 → 提示。
    ///   returnBtn:关闭并回登录页(老端 Fire LOGIN_STATE_CHANGE → Login)。
    ///   UpdateView:每次展示给 account/password 填随机数字(开发期默认值)。
    /// confirmBtn/returnBtn 是 Box 容器,Bind 字段未绑(prefab 里 fileID:0),用 transform.Find 兜底拿命中体内部图。
    /// </summary>
    public sealed class RegisterView : RegisterViewBind
    {
        protected override void OnInit()
        {
        }

        protected override void OnShow(object args)
        {
            // 老端 UpdateView:展示即填随机账号/密码(开发期默认值)。
            account.text = RandomDigits();
            password.text = RandomDigits();

            // 老端 InitEvent。点击每次 OnShow 重绑前先清,防止反复展示叠加监听(对标样板)。
            BindConfirm();
            BindReturn();
        }

        protected override void OnHide()
        {
        }

        protected override void OnDispose()
        {
        }

        /// <summary>确定注册:整个 confirmBtn 盒可点(底图 _Image1 + 文字 _Label3),先清后绑。</summary>
        private void BindConfirm()
        {
            Image hit = ConfirmHit();
            if (hit == null)
            {
                GameLog.Error("Login", "注册页缺 confirmBtn 命中体(烤制 prefab 结构异常?)");
                return;
            }
            hit.raycastTarget = true;
            UIUtil.ClearClicks(hit);
            UIUtil.AddClick(hit, OnClickConfirm);
        }

        /// <summary>返回:整个 returnBtn 盒可点(底图 _Image2 + 文字 _Label4),先清后绑。</summary>
        private void BindReturn()
        {
            Image hit = ReturnHit();
            if (hit == null)
            {
                GameLog.Error("Login", "注册页缺 returnBtn 命中体(烤制 prefab 结构异常?)");
                return;
            }
            hit.raycastTarget = true;
            UIUtil.ClearClicks(hit);
            UIUtil.AddClick(hit, OnClickReturn);
        }

        private void OnClickConfirm()
        {
            // 老端:account/password 任一为空 → 提示后 return,不发协议。
            string acc = (account.text ?? string.Empty).Trim();
            string pwd = (password.text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(acc) || string.IsNullOrEmpty(pwd))
            {
                GameLog.Warn("Login", "请输入账号密码");
                return;
            }
            // 老端 Fire(COMFIRM_REGISTER_BTN) = 注册 + 自动登录;沿用已有 LoginFlow。
            _ = LoginFlow.SubmitRegisterAsync(acc, pwd);
        }

        private void OnClickReturn()
        {
            // 老端:Close + Fire(LOGIN_STATE_CHANGE, Login)。LoginFlow.ShowLogin 已含隐藏注册页 + 显示登录页。
            LoginFlow.ShowLogin();
        }

        // ——— confirmBtn/returnBtn 是 Box 容器,Bind 字段未绑:优先用容器自身的子图作命中体,按名兜底 ———

        /// <summary>confirmBtn 命中体:优先 prefab 已绑的底图 _Image1,否则按 confirmBtn 容器路径找内部图。</summary>
        private Image ConfirmHit()
        {
            if (_Image1 != null) return _Image1;
            Image inner = FindImage(transform, "confirmBtn/_Image1");
            return inner != null ? inner : FindImage(transform, "confirmBtn");
        }

        /// <summary>returnBtn 命中体:优先 prefab 已绑的底图 _Image2,否则按 returnBtn 容器路径找内部图。</summary>
        private Image ReturnHit()
        {
            if (_Image2 != null) return _Image2;
            Image inner = FindImage(transform, "returnBtn/_Image2");
            return inner != null ? inner : FindImage(transform, "returnBtn");
        }

        private static Image FindImage(Transform root, string path)
        {
            Transform t = root.Find(path);
            if (t == null) return null;
            Image self = t.GetComponent<Image>();
            return self != null ? self : t.GetComponentInChildren<Image>(true);
        }

        private static string RandomDigits()
        {
            return UnityEngine.Random.Range(0, 100000).ToString();
        }
    }
}
