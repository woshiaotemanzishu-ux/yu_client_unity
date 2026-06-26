using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Shenxiao.Common.Prefs;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 账号调试登录界面(对标老客户端 login/AccountView.ts):开发用,手填 账号/IP/端口/PID/邀请者/渠道/分辨率/Lburl
    /// 八项连调试服。结构(八个 AccountItem 输入项 + 服务器标签 + 进入/切账号按钮 + IP 地址下拉)已烤进 prefab,
    /// 这里【只绑数据】——不再 new AccountItem、不再 item.display_obj.y 摆位置(老端那套已删)。
    ///
    /// 八项(对标老端 _group_data_ 顺序,与 _gp_con 下八个 View_2 子节点逐一对应):
    ///   invite / fenbianlv / plat_name / account / pid / port / ip / Lburl
    /// 每项 prefab 节点 View_2 内:_lb_desc(TMP 说明文字)+ _ti_value(TMP_InputField 输入值)。
    ///
    /// 降级(新端尚未移植的真·运行时部分,保留并打日志,不臆造):
    ///   - 进入按钮 ClickLoginBtn:老端走 AppConst.SocketAddress/Port + LoginManager.START_GAME_CONNECT 裸 socket 直连,
    ///     新端无对应裸连调试入口(只有走 GM 的 LoginController.DevLoginAsync),这里仅做账号校验 + 存档 + TODO 日志。
    ///   - 切账号按钮:老端 LoginManager.StartAccountLogin,新端无对应入口 → TODO 日志。
    ///   - IP 地址下拉 _scroller_address:老端用 LoginModel.ConfigDebugIpList.ip_list 填充,新端无该配置 → 列表隐藏、不填充。
    /// 帐号项的值用 PlayerPrefs 持久化(对标老端 CookieWrapper.LAST_ACCOUNT_DATA)。
    /// </summary>
    public sealed class AccountView : AccountViewBind
    {
        // 对标老端 _group_data_ 的 key 顺序与说明文字;顺序即 _gp_con 下子节点顺序。
        private static readonly (string key, string desc)[] GroupData =
        {
            ("invite", "邀请者id:"),
            ("fenbianlv", "分辨率:"),
            ("plat_name", "渠道来源:"),
            ("account", "帐号:"),
            ("pid", "PID:"),
            ("port", "Port:"),
            ("ip", "IP:"),
            ("Lburl", "Lburl:"),
        };

        // 对标老端 _need_save_cookies_key_list_:需要存档/回填的项。
        private static readonly string[] SaveCookieKeys =
            { "account", "ip", "port", "pid", "invite", "plat_name", "Lburl" };

        private const string PREF_LAST_ACCOUNT_DATA = "login.last_account_data"; // 对标 CookieWrapper.LAST_ACCOUNT_DATA

        /// <summary>烤进 prefab 的账号项:绑数据时缓存它内部的说明文字 / 输入框。</summary>
        private struct ItemSlot
        {
            public TMP_Text desc;
            public TMP_InputField value;
        }

        // key → 项;按 GroupData 顺序绑定。
        private readonly Dictionary<string, ItemSlot> _items = new Dictionary<string, ItemSlot>();

        protected override void OnInit()
        {
            // 地址下拉:新端无 ConfigDebugIpList,保持隐藏(老端 LoadSuccess 里 _scroller_address.visible = false)。
            if (_scroller_address != null) _scroller_address.gameObject.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            BindBakedItems();
            BindButtons();
            LoadCookiesOrDefault();
        }

        protected override void OnHide()
        {
            // 对标老端 Remove():离开时存档。
            SaveCookies();
        }

        protected override void OnDispose()
        {
        }

        /// <summary>把八项数据绑到烤进 prefab 的账号项上(不再 new AccountItem)。</summary>
        private void BindBakedItems()
        {
            _items.Clear();
            RectTransform con = GpCon();
            if (con == null)
            {
                GameLog.Error("Login", "账号页缺 _gp_con(烤制 prefab 结构异常?)");
                return;
            }

            // 收集 _gp_con 下的账号项(节点名 View_2),按层级序逐一对应 GroupData。
            var rows = new List<Transform>();
            for (int i = 0; i < con.childCount; i++)
            {
                Transform c = con.GetChild(i);
                if (c.name.StartsWith("View_2")) rows.Add(c);
            }
            if (rows.Count < GroupData.Length)
            {
                GameLog.Warn("Login", "烤制账号项 {0} 少于配置项 {1}(配置变了需重烤)", rows.Count, GroupData.Length);
            }

            for (int i = 0; i < GroupData.Length && i < rows.Count; i++)
            {
                Transform row = rows[i];
                var slot = new ItemSlot
                {
                    desc = FindText(row, "_lb_desc"),
                    value = FindInput(row, "_ti_value"),
                };
                if (slot.desc != null) slot.desc.text = GroupData[i].desc; // 对标 AccountItem.UpdateView:_lb_desc.text = data.value.desc
                _items[GroupData[i].key] = slot;
            }
        }

        private void BindButtons()
        {
            // 进入按钮(对标 m_enter_btn → ClickLoginBtn)。每次 OnShow 重绑前先清,防监听叠加。
            BindImageClick(m_enter_btn, OnClickLoginBtn);
            // 切账号按钮(对标 m_account → Close + StartAccountLogin)。
            BindImageClick(m_account, OnClickSwitchAccount);
            // 服务器标签点击(对标 _lb_server → ClickServer:切地址下拉显隐)。
            if (_lb_server != null)
            {
                UIUtil.ClearClicks(_lb_server);
                UIUtil.AddClick(_lb_server, OnClickServer);
            }
        }

        // ——— 对标老端各方法 ———

        /// <summary>对标 ClickServer:切换地址下拉显隐(新端列表为空,仍保留显隐开关语义)。</summary>
        private void OnClickServer()
        {
            if (_scroller_address == null) return;
            bool visible = !_scroller_address.gameObject.activeSelf;
            _scroller_address.gameObject.SetActive(visible);
        }

        /// <summary>对标 ClickLoginBtn:校验账号 → 存档 →(裸 socket 直连未移植)打 TODO。</summary>
        private void OnClickLoginBtn()
        {
            string account = GetItemLabel("account");
            // 老端 regEx:账号只能由数字和英文字母组成。
            if (string.IsNullOrEmpty(account) || !Regex.IsMatch(account, "^[A-Za-z0-9]+$"))
            {
                GameLog.Warn("Login", "账号只能由数字和英文字母组成(account={0})", account);
                return;
            }

            string ip = GetItemLabel("ip");
            int.TryParse(GetItemLabel("port"), out int port);
            int.TryParse(GetItemLabel("pid"), out int pid);
            if (string.IsNullOrEmpty(ip) || port <= 0 || pid < 0)
            {
                GameLog.Warn("Login", "IP/Port/PID 不完整(ip={0} port={1} pid={2})", ip, port, pid);
                return;
            }

            SaveCookies();
            GameLog.Info("Login",
                "账号调试登录:account={0} ip={1} port={2} pid={3} → 待对接裸 socket 直连(老端 AppConst.Socket* + LoginManager.START_GAME_CONNECT,新端暂无对应入口)",
                account, ip, port, pid);
        }

        /// <summary>对标 m_account 点击:Close + LoginManager.StartAccountLogin(新端无对应入口 → TODO)。</summary>
        private void OnClickSwitchAccount()
        {
            SaveCookies();
            Hide();
            GameLog.Info("Login", "切换账号登录 → 待对接 LoginManager.StartAccountLogin");
        }

        /// <summary>对标 LoadSuccess 末段:有存档回填存档,无存档填默认(account 占位 + 分辨率)。</summary>
        private void LoadCookiesOrDefault()
        {
            SetItemLabel("fenbianlv", "720,1280"); // 对标老端固定 SetItemLabel("fenbianlv","720,1280")

            string raw = PrefsManager.GetString(PREF_LAST_ACCOUNT_DATA, string.Empty);
            if (!string.IsNullOrEmpty(raw))
            {
                Dictionary<string, string> dic = null;
                try { dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(raw); }
                catch (Exception e) { GameLog.Warn("Login", "解析账号存档失败:{0}", e.Message); }
                if (dic != null)
                {
                    foreach (KeyValuePair<string, string> kv in dic) SetItemLabel(kv.Key, kv.Value);
                }
            }
            else
            {
                SetItemLabel("account", "请输入帐号");
                // 老端无存档时还会 SetAddressInfo() 从 ConfigDebugIpList 取首个地址写 ip/port/pid;
                // 新端无该配置,跳过(留空待用户手填)。
            }
        }

        /// <summary>对标 SaveCookies:把需存档项序列化进 PlayerPrefs。</summary>
        private void SaveCookies()
        {
            if (_items.Count == 0) return;
            var dic = new Dictionary<string, string>();
            foreach (string key in SaveCookieKeys)
            {
                string v = GetItemLabel(key);
                if (v != null) dic[key] = v;
            }
            PrefsManager.SetString(PREF_LAST_ACCOUNT_DATA, JsonConvert.SerializeObject(dic));
        }

        /// <summary>对标 GetItemLabel:取某项输入框文字(trim)。</summary>
        private string GetItemLabel(string key)
        {
            if (_items.TryGetValue(key, out ItemSlot slot) && slot.value != null)
            {
                return (slot.value.text ?? string.Empty).Trim();
            }
            return null;
        }

        /// <summary>对标 SetItemLabel:写某项输入框文字。</summary>
        private void SetItemLabel(string key, string value)
        {
            if (_items.TryGetValue(key, out ItemSlot slot) && slot.value != null)
            {
                slot.value.text = value ?? string.Empty;
            }
        }

        // ——— 容器字段没绑上时按名兜底(烤制 prefab 里容器节点名与老端一致)———
        private RectTransform GpCon() => _gp_con != null ? _gp_con : transform.Find("_gp_con") as RectTransform;

        private void BindImageClick(Image img, Action onClick)
        {
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.ClearClicks(img); // 每次 OnShow 重绑前先清,防监听叠加
            UIUtil.AddClick(img, onClick);
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform t = root.Find(path);
            if (t == null) return null;
            TMP_Text self = t.GetComponent<TMP_Text>();
            return self != null ? self : t.GetComponentInChildren<TMP_Text>(true);
        }

        private static TMP_InputField FindInput(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<TMP_InputField>() : null;
        }
    }
}
